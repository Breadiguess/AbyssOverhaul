using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AbyssOverhaul.Content.BehaviorOverrides.ReaperShark
{
    internal class ReaperSharkArm
    {
        public IKSkeleton Skeleton;


        public ReaperSharkArm(IKSkeleton skeleton)
        {
            Skeleton = skeleton;
        }

        public static void DrawArm(ReaperSharkArm arm)
        {
            if (arm.Skeleton is null)
                return;

            for(int i = 0; i< arm.Skeleton.JointCount - 1; i++)
            {
                Vector2 start = arm.Skeleton.GetJointPosition(i);
                Vector2 end = arm.Skeleton.GetJointPosition(i + 1);

                Utils.DrawLine(Main.spriteBatch, start, end, Color.White);
            }
        }

    }
}
