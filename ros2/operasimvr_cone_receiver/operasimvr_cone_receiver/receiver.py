"""Replace a complete layout once per request; never append on repeat requests."""
import json
import math
import os
from pathlib import Path

import rclpy
from rclpy.node import Node
from operasimvr_interfaces.srv import SetConeLayout


def validate_layout(request):
    """Return an owned snapshot, or reject without modifying the previous layout."""
    if not request.frame_id or any(c.isspace() for c in request.frame_id):
        raise ValueError('frame_id must be nonempty and contain no whitespace')
    if len(request.groups) != 20 or not 1 <= request.group_count <= 20:
        raise ValueError('exactly 20 slots and 1..20 used groups are required')
    groups = []
    previous_id = 0
    for index, group in enumerate(request.groups):
        if index >= request.group_count:
            if group.group_id != 0 or group.positions:
                raise ValueError('unused slots must have group_id=0 and positions=[]')
            continue
        if group.group_id <= previous_id or not 1 <= len(group.positions) <= 10000:
            raise ValueError('used groups need increasing positive IDs and 1..10000 positions')
        previous_id = group.group_id
        points = []
        for point in group.positions:
            if not all(math.isfinite(v) for v in (point.x, point.y, point.z)):
                raise ValueError('coordinates must be finite')
            points.append({'x': point.x, 'y': point.y, 'z': point.z})
        groups.append({'slot': index, 'group_id': group.group_id, 'positions': points})
    return {'frame_id': request.frame_id, 'group_count': len(groups), 'groups': groups}


class ConeReceiver(Node):
    def __init__(self):
        super().__init__('cone_layout_receiver')
        self.declare_parameter('service_name', '/cone_placement/set_layout')
        self.declare_parameter('output_path', '')
        self.layout = None
        self.service = self.create_service(
            SetConeLayout, self.get_parameter('service_name').value, self.receive)
        self.get_logger().info('Cone receiver ready: ' + self.service.srv_name)

    def receive(self, request, response):
        try:
            snapshot = validate_layout(request)
            output = self.get_parameter('output_path').value
            if output:
                path = Path(output).expanduser()
                path.parent.mkdir(parents=True, exist_ok=True)
                temporary = path.with_name(path.name + '.tmp')
                temporary.write_text(json.dumps(snapshot, indent=2) + '\n', encoding='utf-8')
                os.replace(temporary, path)
            self.layout = snapshot
            response.success = True
            response.message = 'Layout replaced.'
            response.group_count = snapshot['group_count']
            response.cone_count = sum(len(g['positions']) for g in snapshot['groups'])
            summary = ', '.join(
                f"groups[{g['slot']}]=G{g['group_id']} ({len(g['positions'])} cones)"
                for g in snapshot['groups'])
            self.get_logger().info(f"Accepted frame={snapshot['frame_id']}: {summary}")
        except (ValueError, OSError) as error:
            response.success = False
            response.message = str(error)
            response.group_count = 0
            response.cone_count = 0
            self.get_logger().warning('Rejected: ' + str(error))
        return response


def main(args=None):
    rclpy.init(args=args)
    node = ConeReceiver()
    try:
        rclpy.spin(node)
    except KeyboardInterrupt:
        pass
    finally:
        node.destroy_node()
        if rclpy.ok():
            rclpy.shutdown()
