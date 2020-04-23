import numpy as np
import Quaternion as quat

class JointAngleFilter:
    BASIS = { 'x' : np.array( [ 1, 0, 0 ] ),
              'y' : np.array( [ 0, 1, 0 ] ),
              'z' : np.array( [ 0, 0, 1 ] ) }

    @staticmethod
    def sphere( q1, q2, vfo = np.array( [ 0, -1, 0 ] ) ):
        """
        Compute rotation components of a spherical joint

        q1: numpy.ndarray (4,)
            quaternion orientation of pre-joint linkage
        q2: numpy.ndarray (4,)
            quaternion orientation of post-joint linkage
        vfo : numpy.ndarray (3,)
            forward vector of pre-joint linkage (at identity orientation)
        """
        vfo = vfo / np.sqrt( np.sum( np.square( vfo ) ) )   # normalize original forward vector
        # vro = quat.rotate( _, vf )                          # compute right vector (at identity orientation)
        vro = np.array( [ 1, 0, 0 ] )

        qr = q1
        # qr = quat.relative( q1, q2 )        # compute relative rotation
        vf = quat.rotate( qr, vfo )         # compute current forward vector
        
        # print( vfo, vf )
        # print( quat.rotate( quat.between( vfo, vf ), vfo ), vf )
        # exit()

        angles = np.zeros( ( 3, ) )

        # compute shoulder flexion angle
        proj_f = vf - np.dot( vf, JointAngleFilter.BASIS['z'] ) * JointAngleFilter.BASIS['z']       # project forward vector onto sagital plane
        proj_f = proj_f / np.sqrt( np.sum( np.square( proj_f ) ) )                                  # normalize projection
        dotprod = max( -1.0, min( 1.0, np.dot( vfo, proj_f ) ) )                                    # compute dot product, clamped to avoid rounding errors
        angles[0] = np.arccos( dotprod )                                                            # compute angle

        # print( '%.2f --> %.2f' % ( dotprod, angles[0] ) )

        # isolate flexion rotation
        q_flex = quat.between( vfo, proj_f )
        qr_noflex = quat.multiply( quat.inverse( q_flex ), qr )

        # compute shoulder abduction angle
        dotprod = max( -1.0, min( 1.0, np.dot( vf, proj_f ) ) )     # compute dot product, clamped to avoid rounding errors
        angles[1] = np.arccos( dotprod )                            # compute angle

        # print( "%.2f --> %.2f " % ( dotprod, angles[1] ) )

        # isolate abduction rotation
        qr_abd = quat.between( proj_f, vf )
        qr_noflexabd = quat.multiply( quat.inverse( qr_abd ), qr_noflex )

        # compute humeral rotation angle
        vr = quat.rotate( qr_noflexabd, vro )
        proj_r = vr - np.dot( vr, JointAngleFilter.BASIS['z'] ) * JointAngleFilter.BASIS['z']
        proj_r = proj_r / np.sqrt( np.sum( np.square( proj_r ) ) )
        dotprod = max( -1.0, min( 1.0, np.dot( vro, proj_r ) ) )
        angles[2] = np.arccos( dotprod )

        # print( '%.2f --> %.2f' % ( dotprod, angles[2] ) )

        # print( '%.2f\t%.2f\t%.2f' % ( angles[0], angles[1], angles[2] ) )
        # exit()

        return angles

    @staticmethod
    def hinge( q1, q2, vfo = np.array( [ 0, -1, 0 ] ) ):
        """
        Computes the angle of a hinge joint given two quaternion measurements

        Parameters
        ----------
        q1 : numpy.ndarray
            Quaternion orientation of the first device
        q2 : numpy.ndarray
            Quaternion orientation of the second device
        vfo : numpy.ndarray
            Reference forward vector
        
        Returns
        -------
        float
            Angle of the hinge joint
        """
        qr = quat.relative( q1, q2 )
        vf = quat.rotate( q1, vfo )
        angle, axis = quat.to_axis_angle( qr )
        return angle * np.sign( np.dot( vf, axis ) )
    
    def __init__(self, joints = ( 'shoulder', 'elbow', 'wrist' ) ):
        self._joints = joints
        self._n_joints = len( self._joints )
        
        self._n_angles = 0
        if 'shoulder' in self._joints: self._n_angles += 3
        if 'elbow' in self._joints: self._n_angles += 1
        if 'wrist' in self._joints: self._n_angles += 3

    def filter(self, q):
        if len( q.shape ) == 1: q = np.expand_dims( q, axis = 0 )

        n_samples = q.shape[0]
        n_quats = int( q.shape[1] // 4 )

        angles = np.zeros( ( n_samples, self._n_angles ) )

        q = np.split( q, n_quats, axis = 1 )
        for i in range( n_samples ):
            qi = [ quat.normalize( q[j][i,:] ) for j in range( n_quats) ]

            quat_idx, angle_idx = 0, 0
            if 'shoulder' in self._joints:
                angles[i,angle_idx:angle_idx+3] = JointAngleFilter.sphere( qi[quat_idx], qi[quat_idx+1] )
                # quat_idx += 1 # NOTE: this is temporary
                angle_idx += 3
            if 'elbow' in self._joints:
                angles[i,angle_idx:angle_idx+1] = JointAngleFilter.hinge( qi[quat_idx], qi[quat_idx+1] )
                quat_idx += 1
                angle_idx += 1
            if 'wrist' in self._joints:
                angles[i,angle_idx:angle_idx+3] = JointAngleFilter.sphere( qi[quat_idx], qi[quat_idx+1] )
                quat_idx += 1
                angle_idx += 3
        
        return np.squeeze( angles )

if __name__ == '__main__':
    JOINTS = [ 'shoulder', 'elbow', 'wrist' ][0:1]
    N_QUATS = len( JOINTS ) + 1
    N_SAMPLES = 100

    np.random.seed( 0 )

    x = np.random.random( size = ( N_SAMPLES, 4 * N_QUATS ) )
    ja = JointAngleFilter( joints = tuple( JOINTS ) )
    angles = ja.filter( x )
