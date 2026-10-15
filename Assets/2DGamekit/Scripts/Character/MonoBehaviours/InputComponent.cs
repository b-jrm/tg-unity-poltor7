using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Gamekit2D
{
    public abstract class InputComponent : MonoBehaviour
    {
        [Serializable]
        public class InputButton
        {
            public InputAction action;

            public bool Down { get; protected set; }
            public bool Held { get; protected set; }
            public bool Up { get; protected set; }
            public bool Enabled
            {
                get { return m_Enabled; }
            }

            [SerializeField]
            protected bool m_Enabled = true;
            protected bool m_GettingInput = true;

            //This is used to change the state of a button (Down, Up) only if at least a FixedUpdate happened between the previous Frame
            //and this one. Since movement are made in FixedUpdate, without that an input could be missed it get press/release between fixedupdate
            bool m_AfterFixedUpdateDown;
            bool m_AfterFixedUpdateHeld;
            bool m_AfterFixedUpdateUp;

            public InputButton(string actionName, Key keyboardKey, string gamepadBinding)
            {
                action = new InputAction(actionName, InputActionType.Button);
                action.AddBinding($"<Keyboard>/{keyboardKey.ToString().ToLower()}");
                if (!string.IsNullOrEmpty(gamepadBinding))
                    action.AddBinding(gamepadBinding);
            }

            public void Get(bool fixedUpdateHappened)
            {
                if (!m_Enabled)
                {
                    Down = false;
                    Held = false;
                    Up = false;
                    return;
                }

                if (!m_GettingInput)
                    return;

                bool rawDown = action.WasPressedThisFrame();
                bool rawHeld = action.IsPressed();
                bool rawUp = action.WasReleasedThisFrame();

                if (fixedUpdateHappened)
                {
                    Down = rawDown;
                    Held = rawHeld;
                    Up = rawUp;

                    m_AfterFixedUpdateDown = Down;
                    m_AfterFixedUpdateHeld = Held;
                    m_AfterFixedUpdateUp = Up;

                    
                }
                else
                {
                    Down = rawDown || m_AfterFixedUpdateDown;
                    Held = rawHeld || m_AfterFixedUpdateHeld;
                    Up = rawUp || m_AfterFixedUpdateUp;

                    m_AfterFixedUpdateDown |= Down;
                    m_AfterFixedUpdateHeld |= Held;
                    m_AfterFixedUpdateUp |= Up;
                }
            }

            public void Enable()
            {
                m_Enabled = true;
            }

            public void Disable()
            {
                m_Enabled = false;
            }

            public void GainControl()
            {
                m_GettingInput = true;
            }

            public IEnumerator ReleaseControl(bool resetValues)
            {
                m_GettingInput = false;

                if (!resetValues)
                    yield break;

                if (Down)
                    Up = true;
                Down = false;
                Held = false;

                m_AfterFixedUpdateDown = false;
                m_AfterFixedUpdateHeld = false;
                m_AfterFixedUpdateUp = false;

                yield return null;

                Up = false;
            }

            public void EnableAction() => action?.Enable();
            public void DisableAction() => action?.Disable();
        }

        [Serializable]
        public class InputAxis
        {
            public InputAction action;

            public float Value { get; protected set; }
            public bool ReceivingInput { get; protected set; }
            public bool Enabled
            {
                get { return m_Enabled; }
            }

            protected bool m_Enabled = true;
            protected bool m_GettingInput = true;

            public InputAxis(string actionName, Key positiveKey, Key negativeKey, string gamepadPositiveBinding, string gampadNegativeBinding)
            {
                action = new InputAction(actionName, InputActionType.Value, expectedControlType: "Axis");
                
                var composite = action.AddCompositeBinding("1DAxis");
                composite.With("Positive", $"<Keyboard>/{positiveKey.ToString().ToLower()}");
                composite.With("Negative", $"<Keyboard>/{negativeKey.ToString().ToLower()}");

                if (!string.IsNullOrEmpty(gamepadPositiveBinding))
                    action.AddBinding(gamepadPositiveBinding);
            }

            public void Get()
            {
                if (!m_Enabled)
                {
                    Value = 0f;
                    return;
                }

                if (!m_GettingInput)
                    return;

                Value = action.ReadValue<float>();
                ReceivingInput = !Mathf.Approximately(Value, 0f);
            }

            public void Enable()
            {
                m_Enabled = true;
            }

            public void Disable()
            {
                m_Enabled = false;
            }

            public void GainControl()
            {
                m_GettingInput = true;
            }

            public void ReleaseControl(bool resetValues)
            {
                m_GettingInput = false;
                if (resetValues)
                {
                    Value = 0f;
                    ReceivingInput = false;
                }
            }

            public void EnableAction() 
            { 
                action?.Enable(); 
            }

            public void DisableAction() 
            {
                action?.Disable();
            }
        }

        bool m_FixedUpdateHappened;

        void Update()
        {
            GetInputs(m_FixedUpdateHappened || Mathf.Approximately(Time.timeScale, 0));

            m_FixedUpdateHappened = false;
        }

        void FixedUpdate()
        {
            m_FixedUpdateHappened = true;
        }

        protected abstract void GetInputs(bool fixedUpdateHappened);

        public abstract void GainControl();

        public abstract void ReleaseControl(bool resetValues = true);

        protected void GainControl(InputButton inputButton)
        {
            inputButton.GainControl();
        }

        protected void GainControl(InputAxis inputAxis)
        {
            inputAxis.GainControl();
        }

        protected void ReleaseControl(InputButton inputButton, bool resetValues)
        {
            StartCoroutine(inputButton.ReleaseControl(resetValues));
        }

        protected void ReleaseControl(InputAxis inputAxis, bool resetValues)
        {
            inputAxis.ReleaseControl(resetValues);
        }
    }
}
