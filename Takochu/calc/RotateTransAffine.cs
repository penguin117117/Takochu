using OpenTK;
using System;


namespace Takochu.calc
{
    public class RotateTransAffine
    {
        /// <summary>
        /// Specify the axis to be targeted.<br/>
        /// 対象となる軸を指定します。
        /// </summary>
        public enum TargetVector
        {
            /// <summary>
            /// Specify the axis of rotation as the "X" axis.<br/>
            /// 回転軸を "X"軸に指定します。
            /// </summary>
            X,
            /// <summary>
            /// Specify the axis of rotation as the "Y" axis.<br/>
            /// 回転軸を "Y"軸に指定します。
            /// </summary>
            Y,
            /// <summary>
            /// Specify the axis of rotation as the "Z" axis.<br/>
            /// 回転軸を "Z"軸に指定します。
            /// </summary>
            Z,
            /// <summary>
            /// Apply rotation to all X, Y, and Z axes.<br/>
            /// X軸、Y軸、Z軸のすべてに回転を適用します。
            /// </summary>
            All
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="objectTruePosition"></param>
        /// <param name="zoneRot_Degree"></param>
        /// <param name="Axis"></param>
        /// <returns></returns>
        public static Vector3 GetPositionAfterRotation(Vector3 objectTruePosition, Vector3 zoneRot_Degree, TargetVector Axis)
        {
            Vector3 v3Afin = Vector3.Zero;
            switch (Axis)
            {
                case TargetVector.X:
                    v3Afin = AfinX(objectTruePosition, zoneRot_Degree);
                    break;
                case TargetVector.Y:
                    v3Afin = AfinY(objectTruePosition, zoneRot_Degree);
                    break;
                case TargetVector.Z:
                    v3Afin = AfinZ(objectTruePosition, zoneRot_Degree);
                    break;
                case TargetVector.All:
                    v3Afin = AfinAll(objectTruePosition, zoneRot_Degree);
                    break;
            }
            //Console.WriteLine("//////////afin//////////");
            //Console.WriteLine(v3Afin);
            return v3Afin;
        }

        private static Vector3 AfinX(Vector3 globalTruePosition, Vector3 objRot)
        {
            float x = globalTruePosition.X;
            float y = (float)((globalTruePosition.Y * Math.Cos((objRot.X * Math.PI) / 180)) - (globalTruePosition.Z * Math.Sin((objRot.X * Math.PI) / 180)));
            float z = (float)((globalTruePosition.Y * Math.Sin((objRot.X * Math.PI) / 180)) + (globalTruePosition.Z * Math.Cos((objRot.X * Math.PI) / 180)));
            return new Vector3(x, y, z);
        }

        private static Vector3 AfinY(Vector3 globalTruePosition, Vector3 objRot)
        {
            float x = (float)((globalTruePosition.Z * Math.Sin((objRot.Y * Math.PI) / 180)) + (globalTruePosition.X * Math.Cos((objRot.Y * Math.PI) / 180)));
            float y = globalTruePosition.Y;
            float z = (float)((globalTruePosition.Z * Math.Cos((objRot.Y * Math.PI) / 180)) - (globalTruePosition.X * Math.Sin((objRot.Y * Math.PI) / 180)));
            return new Vector3(x, y, z);
        }

        private static Vector3 AfinZ(Vector3 globalTruePosition, Vector3 objRot)
        {
            float x = (float)((globalTruePosition.X * Math.Cos((objRot.Z * Math.PI) / 180)) - (globalTruePosition.Y * Math.Sin((objRot.Z * Math.PI) / 180)));
            float y = (float)((globalTruePosition.X * Math.Sin((objRot.Z * Math.PI) / 180)) + (globalTruePosition.Y * Math.Cos((objRot.X * Math.PI) / 180)));
            float z = globalTruePosition.Z;
            return new Vector3(x, y, z);
        }

        private static Vector3 AfinAll(Vector3 globalTruePosition, Vector3 objRot)
        {
            var AX = GetPositionAfterRotation(globalTruePosition, objRot, TargetVector.X);
            var AY = GetPositionAfterRotation(AX, objRot, TargetVector.Y);
            var AZ = GetPositionAfterRotation(AY, objRot, TargetVector.Z);
            return AZ;
        }

        private static double ToRad(double a)
        {
            return ((a * Math.PI) / 180.0d);
        }

        public static Matrix4 GetGlobalTranslation(Vector3 globalPosition, Vector3 globalRotation)
        {
            float xCos = (float)Math.Cos(ToRad(globalRotation.X));
            float xSin = (float)Math.Sin(ToRad(globalRotation.X));
            float yCos = (float)Math.Cos(ToRad(globalRotation.Y));
            float ySin = (float)Math.Sin(ToRad(globalRotation.Y));
            float zCos = (float)Math.Cos(ToRad(globalRotation.Z));
            float zSin = (float)Math.Sin(ToRad(globalRotation.Z));
            // グローバル→回転方向→XYZ
            Matrix4 translateMat4 = new Matrix4(
                new Vector4(1.0f, 0.0f, 0.0f, globalPosition.X),
                new Vector4(0.0f, 1.0f, 0.0f, globalPosition.Y),
                new Vector4(0.0f, 0.0f, 1.0f, globalPosition.Z),
                new Vector4(0.0f, 0.0f, 0.0f, 1.0f)) // Pos
                * new Matrix4(
                new Vector4(1.0f, 0.0f, 0.0f, 0.0f),
                new Vector4(0.0f, xCos, -xSin, 0.0f),
                new Vector4(0.0f, xSin, xCos, 0.0f),
                new Vector4(0.0f, 0.0f, 0.0f, 1.0f)) // X
                * new Matrix4(
                new Vector4(yCos, 0.0f, ySin, 0.0f),
                new Vector4(0.0f, 1.0f, 0.0f, 0.0f),
                new Vector4(-ySin, 0.0f, yCos, 0.0f),
                new Vector4(0.0f, 0.0f, 0.0f, 1.0f)) // Y
                * new Matrix4(
                new Vector4(zCos, -zSin, 0.0f, 0.0f),
                new Vector4(zSin, zCos, 0.0f, 0.0f),
                new Vector4(0.0f, 0.0f, 1.0f, 0.0f),
                new Vector4(0.0f, 0.0f, 0.0f, 1.0f)); // Z
            return translateMat4;
        }

        // TODO: 計算の最適化。
        public static Quaternion GetQuaternionZYX(Vector3 rotateDec)
        {
            double hX = ToRad(rotateDec.X) / 2;
            double hY = ToRad(rotateDec.Y) / 2;
            double hZ = ToRad(rotateDec.Z) / 2;
            double hXSin = Math.Sin(hX);
            double hYSin = Math.Sin(hY);
            double hZSin = Math.Sin(hZ);
            double hXCos = Math.Cos(hX);
            double hYCos = Math.Cos(hY);
            double hZCos = Math.Cos(hZ);
            return new Quaternion(
                (float)(( hXSin * hYCos * hZCos) - (hXCos * hYSin * hZSin)),
                (float)(( hXSin * hYCos * hZSin) + (hXCos * hYSin * hZCos)),
                (float)((-hXSin * hYSin * hZCos) + (hXCos * hYCos * hZSin)),
                (float)(( hXSin * hYSin * hZSin) + (hXCos * hYCos * hZCos)));
        }

    }
}
