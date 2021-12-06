using System.Diagnostics;
using StereoKit;
using StereoKit.Framework;
using StereoKitApp.HLRuffles;
using StereoKitApp.HLRuffles.SyncStructs;
using StereoKitApp.Utils.EqualityComparers;

namespace StereoKitApp.Features.Avatar
{
    public class Avatar : IStepper
    {
        private class AvatarHand
        {
            internal readonly Stopwatch Stopwatch = new Stopwatch();

            internal SyncVar<HandStruct> SyncHand;
            internal LinePoint[] ThumbLine = new LinePoint[3];
            internal LinePoint[] IndexLine = new LinePoint[3];
            internal LinePoint[] MiddleLine = new LinePoint[3];
            internal LinePoint[] RingLine = new LinePoint[3];
            internal LinePoint[] LittleLine = new LinePoint[3];

            internal HandStruct FromPoints;
            internal HandStruct CurrentPoints;
            internal HandStruct TargetPoints;

            internal bool IsHidden = true;

            public AvatarHand(SyncVar<HandStruct> syncHand)
            {
                SyncHand = syncHand;
            }
        }

        private bool _isOwner;
        private readonly AvatarHand _rightHand;
        private readonly AvatarHand _leftHand;
        private float _tickRate = 20;
        private float _lerpTime = 100;

        private SyncVar<Pose> _syncHead;
        private readonly Stopwatch _headStopwatch = new Stopwatch();
        private Model _headModel = null!;
        private Pose _fromHeadPosition;
        private Pose _currentHeadValue;
        private Pose _headTarget;

        public Avatar(int avatarId, bool isOwner)
        {
            _rightHand = new AvatarHand(new SyncVar<HandStruct>(avatarId, new HandStruct()));
            _leftHand = new AvatarHand(new SyncVar<HandStruct>(avatarId + 1, new HandStruct()));

            _syncHead = new SyncVar<Pose>(avatarId + 2, new Pose(), PoseEqualityComparer.Instance);

            _isOwner = isOwner;

            _rightHand.SyncHand.ValueReceived += UpdateRightHandTarget;
            _leftHand.SyncHand.ValueReceived += UpdateLeftHandTarget;
            _syncHead.ValueReceived += UpdateHeadTarget;

            _rightHand.Stopwatch.Start();
            _leftHand.Stopwatch.Start();
            _headStopwatch.Start();
        }

        private void UpdateRightHandTarget(HandStruct handStruct)
        {
            _rightHand.TargetPoints = handStruct;
            _rightHand.FromPoints = _rightHand.CurrentPoints;
            _rightHand.Stopwatch.Restart();
            _rightHand.IsHidden = false;
        }

        private void UpdateLeftHandTarget(HandStruct handStruct)
        {
            _leftHand.TargetPoints = handStruct;
            _leftHand.FromPoints = _leftHand.CurrentPoints;
            _leftHand.Stopwatch.Restart();
            _leftHand.IsHidden = false;
        }

        private void UpdateHeadTarget(Pose headTarget)
        {
            _fromHeadPosition = _currentHeadValue;
            _headTarget = headTarget;
            _headStopwatch.Restart();
        }

        public bool Initialize()
        {
            ConfigureHand(_rightHand);
            ConfigureHand(_leftHand);

            _headModel = Model.FromFile("Models/Head.glb");

            return true;
        }

        private void ConfigureHand(AvatarHand avatarHand)
        {
            ConfigureFinger(avatarHand.ThumbLine);
            ConfigureFinger(avatarHand.IndexLine);
            ConfigureFinger(avatarHand.MiddleLine);
            ConfigureFinger(avatarHand.RingLine);
            ConfigureFinger(avatarHand.LittleLine);
        }

        private void ConfigureFinger(LinePoint[] finger)
        {
            for (int i = 0; i < finger.Length; i++)
            {
                finger[i].color = new Color32(
                    30,
                    30,
                    30,
                    i < finger.Length - 1 ? (byte)255 : (byte)0
                );
                finger[i].thickness = i < finger.Length - 1 ? 0.01f : 0.005f;
            }
        }

        public void Step()
        {
            if (_isOwner)
            {
                SendHandUpdate(Handed.Right, _rightHand);
                SendHandUpdate(Handed.Left, _leftHand);
                SendHeadUpdate();
            }
            else
            {
                DrawHand(_rightHand);
                DrawHand(_leftHand);
                DrawHead();
            }
        }

        private void DrawHand(AvatarHand avatarHand)
        {
            SetCurrentPoints(avatarHand);
            if (avatarHand.IsHidden)
                return;
            UpdatePoses(avatarHand);

            Lines.Add(avatarHand.ThumbLine);
            Lines.Add(avatarHand.IndexLine);
            Lines.Add(avatarHand.MiddleLine);
            Lines.Add(avatarHand.RingLine);
            Lines.Add(avatarHand.LittleLine);
        }

        private void DrawHead()
        {
            _currentHeadValue = Pose.Lerp(
                _fromHeadPosition,
                _headTarget,
                _headStopwatch.ElapsedMilliseconds / _lerpTime >= 1
                  ? 1
                  : _headStopwatch.ElapsedMilliseconds / _lerpTime
            );
            _headModel.Draw(
                Matrix.TRS(_currentHeadValue.position, _currentHeadValue.orientation, 0.15f),
                Color.White
            );
        }

        private void SetCurrentPoints(AvatarHand avatarHand)
        {
            if (avatarHand.Stopwatch.ElapsedMilliseconds / _lerpTime >= 3)
                avatarHand.IsHidden = true;

            if (avatarHand.Stopwatch.ElapsedMilliseconds / _lerpTime >= 1)
                return;

            avatarHand.CurrentPoints.ThumbTip = Vec3.Lerp(
                avatarHand.FromPoints.ThumbTip,
                avatarHand.TargetPoints.ThumbTip,
                avatarHand.Stopwatch.ElapsedMilliseconds / _lerpTime
            );
            avatarHand.CurrentPoints.ThumbMiddle = Vec3.Lerp(
                avatarHand.FromPoints.ThumbMiddle,
                avatarHand.TargetPoints.ThumbMiddle,
                avatarHand.Stopwatch.ElapsedMilliseconds / _lerpTime
            );
            avatarHand.CurrentPoints.ThumbProximal = Vec3.Lerp(
                avatarHand.FromPoints.ThumbProximal,
                avatarHand.TargetPoints.ThumbProximal,
                avatarHand.Stopwatch.ElapsedMilliseconds / _lerpTime
            );
            avatarHand.CurrentPoints.IndexTip = Vec3.Lerp(
                avatarHand.FromPoints.IndexTip,
                avatarHand.TargetPoints.IndexTip,
                avatarHand.Stopwatch.ElapsedMilliseconds / _lerpTime
            );
            avatarHand.CurrentPoints.IndexMiddle = Vec3.Lerp(
                avatarHand.FromPoints.IndexMiddle,
                avatarHand.TargetPoints.IndexMiddle,
                avatarHand.Stopwatch.ElapsedMilliseconds / _lerpTime
            );
            avatarHand.CurrentPoints.IndexProximal = Vec3.Lerp(
                avatarHand.FromPoints.IndexProximal,
                avatarHand.TargetPoints.IndexProximal,
                avatarHand.Stopwatch.ElapsedMilliseconds / _lerpTime
            );
            avatarHand.CurrentPoints.MiddleTip = Vec3.Lerp(
                avatarHand.FromPoints.MiddleTip,
                avatarHand.TargetPoints.MiddleTip,
                avatarHand.Stopwatch.ElapsedMilliseconds / _lerpTime
            );
            avatarHand.CurrentPoints.MiddleMiddle = Vec3.Lerp(
                avatarHand.FromPoints.MiddleMiddle,
                avatarHand.TargetPoints.MiddleMiddle,
                avatarHand.Stopwatch.ElapsedMilliseconds / _lerpTime
            );
            avatarHand.CurrentPoints.MiddleProximal = Vec3.Lerp(
                avatarHand.FromPoints.MiddleProximal,
                avatarHand.TargetPoints.MiddleProximal,
                avatarHand.Stopwatch.ElapsedMilliseconds / _lerpTime
            );
            avatarHand.CurrentPoints.RingTip = Vec3.Lerp(
                avatarHand.FromPoints.RingTip,
                avatarHand.TargetPoints.RingTip,
                avatarHand.Stopwatch.ElapsedMilliseconds / _lerpTime
            );
            avatarHand.CurrentPoints.RingMiddle = Vec3.Lerp(
                avatarHand.FromPoints.RingMiddle,
                avatarHand.TargetPoints.RingMiddle,
                avatarHand.Stopwatch.ElapsedMilliseconds / _lerpTime
            );
            avatarHand.CurrentPoints.RingProximal = Vec3.Lerp(
                avatarHand.FromPoints.RingProximal,
                avatarHand.TargetPoints.RingProximal,
                avatarHand.Stopwatch.ElapsedMilliseconds / _lerpTime
            );
            avatarHand.CurrentPoints.LittleTip = Vec3.Lerp(
                avatarHand.FromPoints.LittleTip,
                avatarHand.TargetPoints.LittleTip,
                avatarHand.Stopwatch.ElapsedMilliseconds / _lerpTime
            );
            avatarHand.CurrentPoints.LittleMiddle = Vec3.Lerp(
                avatarHand.FromPoints.LittleMiddle,
                avatarHand.TargetPoints.LittleMiddle,
                avatarHand.Stopwatch.ElapsedMilliseconds / _lerpTime
            );
            avatarHand.CurrentPoints.LittleProximal = Vec3.Lerp(
                avatarHand.FromPoints.LittleProximal,
                avatarHand.TargetPoints.LittleProximal,
                avatarHand.Stopwatch.ElapsedMilliseconds / _lerpTime
            );
        }

        private void UpdatePoses(AvatarHand avatarHand)
        {
            avatarHand.ThumbLine[0].pt = avatarHand.CurrentPoints.ThumbTip;
            avatarHand.ThumbLine[1].pt = avatarHand.CurrentPoints.ThumbMiddle;
            avatarHand.ThumbLine[2].pt = avatarHand.CurrentPoints.ThumbProximal;

            avatarHand.IndexLine[0].pt = avatarHand.CurrentPoints.IndexTip;
            avatarHand.IndexLine[1].pt = avatarHand.CurrentPoints.IndexMiddle;
            avatarHand.IndexLine[2].pt = avatarHand.CurrentPoints.IndexProximal;

            avatarHand.MiddleLine[0].pt = avatarHand.CurrentPoints.MiddleTip;
            avatarHand.MiddleLine[1].pt = avatarHand.CurrentPoints.MiddleMiddle;
            avatarHand.MiddleLine[2].pt = avatarHand.CurrentPoints.MiddleProximal;

            avatarHand.RingLine[0].pt = avatarHand.CurrentPoints.RingTip;
            avatarHand.RingLine[1].pt = avatarHand.CurrentPoints.RingMiddle;
            avatarHand.RingLine[2].pt = avatarHand.CurrentPoints.RingProximal;

            avatarHand.LittleLine[0].pt = avatarHand.CurrentPoints.LittleTip;
            avatarHand.LittleLine[1].pt = avatarHand.CurrentPoints.LittleMiddle;
            avatarHand.LittleLine[2].pt = avatarHand.CurrentPoints.LittleProximal;
        }

        private void SendHandUpdate(Handed hand, AvatarHand avatarHand)
        {
            if (!Input.Hand(hand).IsTracked)
                return;
            if (avatarHand.Stopwatch.ElapsedMilliseconds < 1000 / _tickRate)
                return;
            avatarHand.Stopwatch.Restart();

            avatarHand.SyncHand.Value = GetCurrentHandPosition(hand);
        }

        private void SendHeadUpdate()
        {
            if (_headStopwatch.ElapsedMilliseconds < 1000 / _tickRate)
                return;
            _headStopwatch.Restart();

            _syncHead.Value = Input.Head;
        }

        private HandStruct GetCurrentHandPosition(Handed hand) =>
            new HandStruct
            {
                ThumbTip = Input.Hand(hand).Get(FingerId.Thumb, JointId.Tip).position,
                IndexTip = Input.Hand(hand).Get(FingerId.Index, JointId.Tip).position,
                MiddleTip = Input.Hand(hand).Get(FingerId.Middle, JointId.Tip).position,
                RingTip = Input.Hand(hand).Get(FingerId.Ring, JointId.Tip).position,
                LittleTip = Input.Hand(hand).Get(FingerId.Little, JointId.Tip).position,
                ThumbMiddle = Input.Hand(hand).Get(FingerId.Thumb, JointId.KnuckleMid).position,
                IndexMiddle = Input.Hand(hand).Get(FingerId.Index, JointId.KnuckleMid).position,
                MiddleMiddle = Input.Hand(hand).Get(FingerId.Middle, JointId.KnuckleMid).position,
                RingMiddle = Input.Hand(hand).Get(FingerId.Ring, JointId.KnuckleMid).position,
                LittleMiddle = Input.Hand(hand).Get(FingerId.Little, JointId.KnuckleMid).position,
                ThumbProximal = Input.Hand(hand).Get(FingerId.Thumb, JointId.KnuckleMajor).position,
                IndexProximal = Input.Hand(hand).Get(FingerId.Index, JointId.KnuckleMajor).position,
                MiddleProximal =
                    Input.Hand(hand).Get(FingerId.Middle, JointId.KnuckleMajor).position,
                RingProximal = Input.Hand(hand).Get(FingerId.Ring, JointId.KnuckleMajor).position,
                LittleProximal =
                    Input.Hand(hand).Get(FingerId.Little, JointId.KnuckleMajor).position
            };

        public void Shutdown()
        {
            _rightHand.SyncHand.ValueReceived -= UpdateRightHandTarget;
            _leftHand.SyncHand.ValueReceived -= UpdateLeftHandTarget;
            _syncHead.ValueReceived -= UpdateHeadTarget;

            _rightHand.SyncHand.Dispose();
            _leftHand.SyncHand.Dispose();
            _syncHead.Dispose();
        }

        public bool Enabled { get; }
    }
}
