using UnityEngine;
using UnityEngine.InputSystem;

namespace Gamekit2D
{
    public class PlayerInput : InputComponent, IDataPersister
    {
        public static PlayerInput Instance
        {
            get { return s_Instance; }
        }

        protected static PlayerInput s_Instance;

        public bool HaveControl { get { return m_HaveControl; }}

        public InputButton Jump = new InputButton("Jump", Key.Space, "<Gamepad>/buttonSouth");
        public InputButton MeleeAttack = new InputButton("MeleeAttack", Key.K, "<Gamepad>/buttonWest");
        public InputButton RangedAttack = new InputButton("RangedAttack", Key.O, "<Gamepad>/buttonEast");
        public InputButton Interact = new InputButton("Interact", Key.E, "<Gamepad>/buttonNorth");
        public InputButton Pause = new InputButton("Pause", Key.Escape, "<Gamepad>/start");
        public InputAxis Horizontal = new InputAxis("Horizontal", Key.D, Key.A, "<Gamepad>/leftStick/right", "<Gamepad>/leftStick/left");
        public InputAxis Vertical = new InputAxis("Vertical", Key.W, Key.S, "<Gamepad>/leftStick/up", "<Gamepad>/leftStick/down");

        [HideInInspector] public DataSettings dataSettings;

        protected bool m_HaveControl = true;
        protected bool m_DebugMenuOpen = false;

        void Awake()
        {
            if (s_Instance == null)
                s_Instance = this;
            else
                throw new UnityException(
                    "There cannot be more than one PlayerInput script. " +
                    "The instances are " + s_Instance.name + " and " + name + ".");

            EnableAllActions();
        }

        private void OnEnable()
        {
            if (s_Instance == null)
                s_Instance = this;
            else if (s_Instance != this)
                throw new UnityException(
                    "There cannot be more than one PlayerInput script. " +
                    "The instances are " + s_Instance.name + " and " + name + ".");

            PersistentDataManager.RegisterPersister(this);
            EnableAllActions();
        }

        private void OnDisable()
        {
            PersistentDataManager.UnregisterPersister(this);
            DisableAllActions();
            s_Instance = null;
        }

        protected override void GetInputs(bool fixedUpdateHappened)
        {
            Pause.Get(fixedUpdateHappened);
            Interact.Get(fixedUpdateHappened);
            MeleeAttack.Get(fixedUpdateHappened);
            RangedAttack.Get(fixedUpdateHappened);
            Jump.Get(fixedUpdateHappened);
            Horizontal.Get();
            Vertical.Get();

            if (Keyboard.current != null && Keyboard.current.f12Key.wasPressedThisFrame)
                m_DebugMenuOpen = !m_DebugMenuOpen;
        }

        public override void GainControl()
        {
            m_HaveControl = true;

            GainControl(Pause);
            GainControl(Interact);
            GainControl(MeleeAttack);
            GainControl(RangedAttack);
            GainControl(Jump);
            GainControl(Horizontal);
            GainControl(Vertical);
        }

        public override void ReleaseControl(bool resetValues = true)
        {
            m_HaveControl = false;

            ReleaseControl(Pause, resetValues);
            ReleaseControl(Interact, resetValues);
            ReleaseControl(MeleeAttack, resetValues);
            ReleaseControl(RangedAttack, resetValues);
            ReleaseControl(Jump, resetValues);
            ReleaseControl(Horizontal, resetValues);
            ReleaseControl(Vertical, resetValues);
        }

        public void DisableMeleeAttacking()
        {
            MeleeAttack.Disable();
        }

        public void EnableMeleeAttacking()
        {
            MeleeAttack.Enable();
        }

        public void DisableRangedAttacking()
        {
            RangedAttack.Disable();
        }

        public void EnableRangedAttacking()
        {
            RangedAttack.Enable();
        }

        public DataSettings GetDataSettings()
        {
            return dataSettings;
        } 

        public void SetDataSettings(string dataTag, DataSettings.PersistenceType type)
        {
            dataSettings.dataTag = dataTag;
            dataSettings.persistenceType = type;
        }

        public Data SaveData()
        {
            return new Data<bool, bool>(MeleeAttack.Enabled, RangedAttack.Enabled);
        }

        public void LoadData(Data data)
        {
            var d = (Data<bool, bool>)data;

            if (d.value0) MeleeAttack.Enable(); else MeleeAttack.Disable();
            if (d.value1) RangedAttack.Enable(); else RangedAttack.Disable();
        }

        void OnGUI()
        {
            if (m_DebugMenuOpen)
            {
                const float height = 100f;

                GUILayout.BeginArea(new Rect(30, Screen.height - height, 200, height));

                GUILayout.BeginVertical("box");
                GUILayout.Label("Press F12 to close");

                bool meleeEnabled = GUILayout.Toggle(MeleeAttack.Enabled, "Enable Melee Attack");
                bool rangeEnabled = GUILayout.Toggle(RangedAttack.Enabled, "Enable Range Attack");

                if (meleeEnabled != MeleeAttack.Enabled)
                {
                    if (meleeEnabled) 
                        MeleeAttack.Enable();
                    else 
                        MeleeAttack.Disable();
                }

                if (rangeEnabled != RangedAttack.Enabled)
                {
                    if (rangeEnabled) 
                        RangedAttack.Enable();
                    else 
                        RangedAttack.Disable();
                }
                GUILayout.EndVertical();
                GUILayout.EndArea();
            }
        }

        void EnableAllActions()
        {
            Pause.EnableAction();
            Interact.EnableAction();
            MeleeAttack.EnableAction();
            RangedAttack.EnableAction();
            Jump.EnableAction();
            Horizontal.EnableAction();
            Vertical.EnableAction();
        }

        void DisableAllActions()
        {
            Pause.DisableAction();
            Interact.DisableAction();
            MeleeAttack.DisableAction();
            RangedAttack.DisableAction();
            Jump.DisableAction();
            Horizontal.DisableAction();
            Vertical.DisableAction();
        }
    }
}
