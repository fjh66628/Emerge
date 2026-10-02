using UnityEngine.InputSystem;

namespace MVP03
{
    public static class PlayerKeyboard
    {
        public static Keyboard Current
        {
            get
            {
                Keyboard current = Keyboard.current;
                if (IsAvailable(current) && current.native) return current;

                // Editor input tests can leave a virtual device current. Keep hardware usable,
                // and never read a stale device that has already been removed from InputSystem.
                foreach (InputDevice device in InputSystem.devices)
                    if (device is Keyboard keyboard && keyboard.native && IsAvailable(keyboard))
                        return keyboard;

                // Virtual keyboards are still supported when running isolated input tests.
                return IsAvailable(current) ? current : null;
            }
        }

        private static bool IsAvailable(Keyboard keyboard) =>
            keyboard != null && keyboard.added && keyboard.enabled;
    }
}
