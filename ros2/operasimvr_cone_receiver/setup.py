from glob import glob
from setuptools import setup

package_name = 'operasimvr_cone_receiver'
setup(
    name=package_name,
    version='0.1.0',
    packages=[package_name],
    data_files=[
        ('share/ament_index/resource_index/packages', ['resource/' + package_name]),
        ('share/' + package_name, ['package.xml']),
        ('share/' + package_name + '/launch', glob('launch/*.launch.py')),
    ],
    install_requires=['setuptools'],
    zip_safe=True,
    maintainer='OperaSimVR maintainers',
    maintainer_email='maintainers@example.com',
    description='Example receiver for a fixed-capacity cone layout service.',
    license='Apache-2.0',
    entry_points={'console_scripts': [
        'cone_receiver = operasimvr_cone_receiver.receiver:main',
        'cone_endpoint = operasimvr_cone_receiver.endpoint:main',
    ]},
)
