"""A separate endpoint, with a ROS client discovered before accepting Unity requests."""
import rclpy
from ros_tcp_endpoint import TcpServer


def main(args=None):
    rclpy.init(args=args)
    server = TcpServer('ConeEndpoint')
    server.declare_parameter('service_name', '/cone_placement/set_layout')
    service_name = server.get_parameter('service_name').value
    # ROS-TCP-Endpoint v0.7 initializes this in setup_executor; registration precedes it.
    server.executor = None
    server.syscommands.ros_service(service_name, 'operasimvr_interfaces/SetConeLayout')
    client = server.ros_services_table[service_name].cli
    server.get_logger().info('Waiting for cone receiver: ' + service_name)
    try:
        while rclpy.ok() and not client.wait_for_service(timeout_sec=1.0):
            pass
        if rclpy.ok():
            server.get_logger().info('Cone service ready; accepting one-shot Unity sends.')
            server.start()
            server.setup_executor()
    except KeyboardInterrupt:
        pass
    finally:
        server.destroy_nodes()
        if rclpy.ok():
            rclpy.shutdown()
