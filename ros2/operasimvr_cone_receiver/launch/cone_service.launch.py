from launch import LaunchDescription
from launch.actions import DeclareLaunchArgument
from launch.substitutions import LaunchConfiguration
from launch_ros.actions import Node
from launch_ros.parameter_descriptions import ParameterValue


def generate_launch_description():
    service = LaunchConfiguration('service_name')
    return LaunchDescription([
        DeclareLaunchArgument('service_name', default_value='/cone_placement/set_layout'),
        DeclareLaunchArgument('tcp_ip', default_value='127.0.0.1'),
        DeclareLaunchArgument('tcp_port', default_value='10001'),
        DeclareLaunchArgument('output_path', default_value=''),
        Node(package='operasimvr_cone_receiver', executable='cone_receiver',
             parameters=[{'service_name': service, 'output_path': LaunchConfiguration('output_path')}],
             output='screen'),
        Node(package='operasimvr_cone_receiver', executable='cone_endpoint',
             parameters=[{'service_name': service, 'ROS_IP': LaunchConfiguration('tcp_ip'),
                          'ROS_TCP_PORT': ParameterValue(LaunchConfiguration('tcp_port'), value_type=int)}],
             output='screen'),
    ])
