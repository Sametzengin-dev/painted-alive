using FishNet.Broadcast;
using UnityEngine;

namespace PaintedAlive.Networking.M56
{
    public struct M56HelloBroadcast : IBroadcast
    {
        public ulong SteamId;
        public string PersonaName;
    }

    public struct M56RoleRequestBroadcast : IBroadcast
    {
        public byte DesiredRole;
        public int KnownRevision;
    }

    public struct M56RoleStateBroadcast : IBroadcast
    {
        public int SessionGeneration;
        public int Revision;
        public int FigureClientId;
        public int PainterClientId;
        public Vector3 FigurePosition;
        public Quaternion FigureRotation;
        public Vector3 FigureVelocity;
        public string FigurePlayerName;
        public string PainterPlayerName;
        public string Reason;
    }

    public struct M56FigureStateBroadcast : IBroadcast
    {
        public int SessionGeneration;
        public uint Sequence;
        public int RoleRevision;
        public int OriginClientId;
        public Vector3 Position;
        public Quaternion Rotation;
        public Vector3 Velocity;
    }

    public struct M56WorldActionRequestBroadcast : IBroadcast
    {
        public int SessionGeneration;
        public uint RequestId;
        public int KnownRoleRevision;
        public string SystemReference;
    }

    public struct M56WorldActionCommitBroadcast : IBroadcast
    {
        public int SessionGeneration;
        public uint ActionRevision;
        public int RoleRevision;
        public int OriginClientId;
        public string SystemReference;
    }

    public struct M56GameplayFeedbackBroadcast : IBroadcast
    {
        public uint RequestId;
        public bool Accepted;
        public string Message;
    }

    public struct M56OilStrokeBroadcast : IBroadcast
    {
        public int SessionGeneration;
        public uint Sequence;
        public int RoleRevision;
        public int OriginClientId;
        public int Shape;
        public Vector3[] Points;
        public float DrawSpeed;
        public float Pressure;
        public float Width;
        public float Height;
        public float Pigment;
        public float CutResistance;
        public float LifecycleDuration;
        public float Budget;
    }

    public struct M56OilClearBroadcast : IBroadcast
    {
        public int SessionGeneration;
        public uint Sequence;
        public int RoleRevision;
        public int OriginClientId;
    }

    public struct M56InkCreatureBroadcast : IBroadcast
    {
        public int SessionGeneration;
        public uint Sequence;
        public int RoleRevision;
        public int OriginClientId;
        public int LoadoutId;
        public Vector3 Point;
        public Vector3 Normal;
        public Vector3 Facing;
    }

    public struct M56RoundResetBroadcast : IBroadcast
    {
        public int SessionGeneration;
        public int RoleRevision;
        public Vector3 FigurePosition;
        public Quaternion FigureRotation;
    }
}
