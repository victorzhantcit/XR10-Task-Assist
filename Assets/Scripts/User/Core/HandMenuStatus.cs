namespace User.Core
{
    public enum HandMenuView
    {
        SignOut,
        Inspector,
        Worker,
        Staff
    }

    public class HandMenuStatus : EnumStateVisualizer<HandMenuView> 
    {
        public void SetSignOutView() => base.SetEnumValue(HandMenuView.SignOut);
        public void SetInspectorView() => base.SetEnumValue(HandMenuView.Inspector);
        public void SetWorkerView() => base.SetEnumValue(HandMenuView.Worker);
    }
}
