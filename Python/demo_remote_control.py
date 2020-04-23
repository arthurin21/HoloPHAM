import time
import numpy as np

# from HapticController import HapticController
# from HoloTactilePercepts import HoloTactilePercepts

from InertialMeasurementUnits import InertialMeasurementUnits
from CyberGlove import CyberGlove
from HoloModularProstheticLimb import HoloModularProstheticLimb

from JointAngleFilter import JointAngleFilter

HOLOLENS_IP = '127.0.0.1'
HOLOLENS_PORT = 9027

IMU_CALIBRATION_TIME = 10
CYBERGLOVE_CALIBRATION_TIME = 15

IMU_JOINTS = ( 'shoulder', 'elbow' )
MPL_JOINT_RANGE = { 'finger_mcp' : ( -30.0, 110.0 ),
                    'thumb_mcp'  : (   0.0,  68.0 ),
                    'thumb_abd'  : (   0.0, 105.0 ), 
                    'wrist_flex' : ( -60.0,  60.0 ),
                    'wrist_dev'  : ( -15.0,  45.0 ),
                    'wrist_rot'  : ( -90.0,  90.0) }

if __name__ == '__main__':
    print( "Creating IMU interface...", end = '', flush = True )
    imu = InertialMeasurementUnits( com = 'COM13', chan = [ 4, 5 ] )
    print( 'Done!' )

    print( "Creating CyberGlove interface...", end = '', flush = True )
    cg = CyberGlove( com = 'COM12' )
    print( 'Done!' )

    print( "Creating joint angle filter...", end = '', flush = True )
    ja = JointAngleFilter( joints = IMU_JOINTS )
    print( 'Done!' )

    print( "Creating hololens MPL interface...", end = '', flush = True )
    mpl = HoloModularProstheticLimb( ip = HOLOLENS_IP, port = HOLOLENS_PORT )
    print( 'Done!' )

    input( "Press ENTER to calibrate IMU rest orientation..." )
    print( "Please remain still...", end = '', flush = True )
    imu.set_calibrate( calibration_count = 100 )
    print( "Done!" )

    # print( "Calibrate IMUs for forward control...", flush = True )
    
    # print( "Please flex your shoulder 90°...", end = '', flush = True )
    # t0 = time.perf_counter()
    # while time.perf_counter() - t0 < IMU_CALIBRATION_TIME:
    #     pass
    # print( 'Done!' )

    # print( "Please abduct your shoulder 90°...", end = ''. flush = True )
    # t0 = time.perf_counter()
    # while time.perf_counter() - t0 < IMU_CALIBRATION_TIME:
    #     pass
    # print( 'Done!')    

    # print( "IMU calibration complete!" )

    input( "Press ENTER to calibrate range of CyberGlove..." )
    print( 'Explore min/max range for CyberGlove...', end = '', flush = True )
    cg.set_calibrate( timeout = CYBERGLOVE_CALIBRATION_TIME )
    print( 'Done!' )

    input( "Press ENTER to begin streaming..." )
    print( 'Streaming...' )
    # NOTE: WE ARE NOT CONTROLLING WRIST ROTATION
    imu.run()
    cg.run()
    try:
        angles = [ 0.0 ] * HoloModularProstheticLimb.NUM_JOINT_ANGLES # initialize joint angles
        # angles = np.zeros( ( HoloModularProstheticLimb.NUM_JOINT_ANGLES, ) )    # initialize joint angles
        while True:
            imu_state = imu.state
            if imu_state is not None:    
                # compute shoulder / elbow angles from IMU
                imu_angles = np.rad2deg( ja.filter( imu_state ) )
                angles[0] = imu_angles[0]
                angles[1] = -imu_angles[1]
                angles[2] = imu_angles[2]
                angles[3] = -imu_angles[3]
                
                print( '%.2f\t%.2f\t%.2f\t%.2f' % ( angles[0], angles[1], angles[2], angles[3] ) )
            cg_state = cg.state
            if cg_state is not None:
                # finger angles from CyberGlove
                thumb  = cg_state[2]  * ( MPL_JOINT_RANGE['thumb_mcp'][1] - MPL_JOINT_RANGE['thumb_mcp'][0] ) + MPL_JOINT_RANGE['thumb_mcp'][0]
                index  = cg_state[5]  * ( MPL_JOINT_RANGE['finger_mcp'][1] - MPL_JOINT_RANGE['finger_mcp'][0] ) + MPL_JOINT_RANGE['finger_mcp'][0]
                middle = cg_state[8]  * ( MPL_JOINT_RANGE['finger_mcp'][1] - MPL_JOINT_RANGE['finger_mcp'][0] ) + MPL_JOINT_RANGE['finger_mcp'][0]
                ring   = cg_state[12] * ( MPL_JOINT_RANGE['finger_mcp'][1] - MPL_JOINT_RANGE['finger_mcp'][0] ) + MPL_JOINT_RANGE['finger_mcp'][0]
                little = cg_state[16] * ( MPL_JOINT_RANGE['finger_mcp'][1] - MPL_JOINT_RANGE['finger_mcp'][0] ) + MPL_JOINT_RANGE['finger_mcp'][0]

                # wrist angles from CyberGlove
                wrist_flex = cg_state[20] * ( MPL_JOINT_RANGE['wrist_flex'][1] - MPL_JOINT_RANGE['wrist_flex'][0] ) + MPL_JOINT_RANGE['wrist_flex'][0]
                wrist_dev  = cg_state[21] * ( MPL_JOINT_RANGE['wrist_dev'][1] - MPL_JOINT_RANGE['wrist_dev'][0] ) + MPL_JOINT_RANGE['wrist_dev'][0]
            
                # thumb abduction
                # TODO: NOT STRICTLY CORRECT BUT MAY WORK
                thumb_abd = cg_state[2] * ( MPL_JOINT_RANGE['thumb_abd'][1] - MPL_JOINT_RANGE['thumb_abd'][0] ) + MPL_JOINT_RANGE['thumb_abd'][0]

                # update angles
                angles[5] = wrist_dev
                angles[6] = wrist_flex

                angles[8]  = angles[9]  = angles[10] = index
                angles[12] = angles[13] = angles[14] = middle
                angles[16] = angles[17] = angles[18] = ring
                angles[20] = angles[21] = angles[22] = little
                angles[24] = angles[25] = angles[26] = thumb
                angles[23] = 90 # thumb_abd

            mpl.publish( joint_angles = angles )
    finally:
        cg.stop()
        # imu.stop()

