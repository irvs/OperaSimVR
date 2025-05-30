# OperaSimVR(PhysX)
Simulator on Unity + PhysX communicating with ROS

## Explanation
- This system serves as the cyber component of ros2-tms for construction(https://github.com/irvs/ros2_tms_for_construction), a cyber-physical system (CPS) designed for earthwork construction sites.
- This system is an enhanced version of OperaSim-PhysX(https://github.com/pwri-opera/OperaSim-PhysX), the official simulator of the autonomous construction technology development platform OPERA (Open Platform for Earth work with Robotics and Autonomy).
- This system has "nomal mode" and "play mode", "control mode", "prev mode". "Normal mode" is a simulator similar to OperaSim. In "Play mode," the system receives topics such as position, orientation, and joint angles from the actual machine, allowing its movements to be visualized within the server environment. In "Control mode," the system can remotely operate the actual machine by sending command topics. In "Preview mode," the system allows for remote operation of the actual machine while simultaneously performing motion simulation using the simulator's functionalities.



![Videotogif](https://user-images.githubusercontent.com/24404939/159425467-c244de28-354e-4d2a-a615-5ccafc7b9709.gif)

## インストール方法

 #### ROSと連携時の送受信データ
