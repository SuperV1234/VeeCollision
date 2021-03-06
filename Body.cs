#region
using System;
using System.Collections.Generic;
using System.Linq;
using SFMLStart.Vectors;

#endregion

namespace VeeCollision
{
    public class Body
    {
        private readonly bool _isStatic;

        public Body(World mWorld, SSVector2I mPosition, bool mIsStatic, int mWidth, int mHeight)
        {
            World = mWorld;
            Position = mPosition;
            _isStatic = mIsStatic;
            HalfSize = new SSVector2I(mWidth/2, mHeight/2);

            Cells = new HashSet<Cell>();
            Groups = new HashSet<int>();
            GroupsToCheck = new HashSet<int>();
            GroupsToIgnoreResolve = new HashSet<int>();
        }

        #region Properties
        public World World { get; set; }
        public HashSet<Cell> Cells { get; set; }
        public SSVector2I Position { get; set; }
        public SSVector2I PreviousPosition { get; private set; }
        public SSVector2I Velocity { get; set; }
        public SSVector2I HalfSize { get; set; }
        public HashSet<int> Groups { get; private set; }
        public HashSet<int> GroupsToCheck { get; private set; }
        public HashSet<int> GroupsToIgnoreResolve { get; private set; }
        public Action<CollisionInfo> OnCollision { get; set; }
        public Action OnOutOfBounds { get; set; }
        public object UserData { get; set; }
        #endregion

        #region Shortcut Properties
        public int X { get { return Position.X; } }
        public int Y { get { return Position.Y; } }
        public int Left { get { return Position.X - HalfSize.X; } }
        public int Right { get { return Position.X + HalfSize.X; } }
        public int Top { get { return Position.Y - HalfSize.Y; } }
        public int Bottom { get { return Position.Y + HalfSize.Y; } }
        public int HalfWidth { get { return HalfSize.X; } }
        public int HalfHeight { get { return HalfSize.Y; } }
        public int Width { get { return HalfSize.X*2; } }
        public int Height { get { return HalfSize.Y*2; } }
        #endregion

        #region Group-related methods
        public void AddGroups(params int[] mGroups) { foreach (var group in mGroups) Groups.Add(group); }
        public void AddGroupsToCheck(params int[] mGroups) { foreach (var group in mGroups) GroupsToCheck.Add(group); }
        public void AddGroupsToIgnoreResolve(params int[] mGroups) { foreach (var group in mGroups) GroupsToIgnoreResolve.Add(group); }
        #endregion

        private bool IsOverlapping(Body mBody) { return Right > mBody.Left && Left < mBody.Right && (Bottom > mBody.Top && Top < mBody.Bottom); }

        public void Update(float mFrameTime)
        {
            if (_isStatic) return;

            PreviousPosition = Position;

            var tempVelocity = new SSVector2F(Velocity.X*mFrameTime, Velocity.Y*mFrameTime);
            var tempPosition = new SSVector2F(Position.X + tempVelocity.X, Position.Y + tempVelocity.Y);

            Position = new SSVector2I((int) tempPosition.X, (int) tempPosition.Y);

            var checkedBodies = new HashSet<Body> {this};
            var bodiesToCheck = World.GetBodies(this);

            foreach (var body in bodiesToCheck.OrderBy(x => Velocity.X > 0 ? x.X : -x.X))
            {
                if (checkedBodies.Contains(body)) continue;
                checkedBodies.Add(body);

                if (!IsOverlapping(body)) continue;

                if (OnCollision != null) OnCollision(new CollisionInfo(mFrameTime, body.UserData, body));
                if (body.OnCollision != null) body.OnCollision(new CollisionInfo(mFrameTime, UserData, this));

                if (GroupsToIgnoreResolve.Any(x => body.Groups.Contains(x))) continue;

                int encrX = 0, encrY = 0;

                if (Bottom < body.Bottom && Bottom >= body.Top) encrY = body.Top - Bottom;
                else if (Top > body.Top && Top <= body.Bottom) encrY = body.Bottom - Top;

                if (Left < body.Left && Right >= body.Left) encrX = body.Left - Right;
                else if (Right > body.Right && Left <= body.Right) encrX = body.Right - Left;

                var overlapX = Left < body.Left ? Right - body.Left : body.Right - Left;
                var overlapY = Top < body.Top ? Bottom - body.Top : body.Bottom - Top;

                Position += overlapX > overlapY ? new SSVector2I(0, encrY) : new SSVector2I(encrX, 0);
            }

            World.UpdateBody(this);
        }
    }
}