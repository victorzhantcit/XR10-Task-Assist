namespace Guidance.Dtos
{
    public enum WebsocketType
    {
        GameViewEncoder,
        GameViewDecoder,
        RenameRoom,
        ClientDisconnectNotify,
        TransferRoomMaster,
        RemoteMarker,
        UndoMarker,
        RedoMarker,
        EraseAllMarker
    }
}
