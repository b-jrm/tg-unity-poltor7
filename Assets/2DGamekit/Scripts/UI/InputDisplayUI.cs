using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Gamekit2D
{
    public class InputDisplayUI : MonoBehaviour
    {
        private void OnEnable()
        {
            TextMeshProUGUI textUI = GetComponent<TextMeshProUGUI>();
            if (PlayerInput.Instance == null)
            {
                textUI.SetText("## ERROR ## No PlayerInput detected");
                return;
            }

            StringBuilder builder = new StringBuilder();

            builder.AppendFormat("{0} - Move Left\n", GetKeyboardNegative(PlayerInput.Instance.Horizontal.action));
            builder.AppendFormat("{0} - Move Right\n", GetKeyboardPositive(PlayerInput.Instance.Horizontal.action));
            builder.AppendFormat("{0} - Look Up\n", GetKeyboardPositive(PlayerInput.Instance.Vertical.action));
            builder.AppendFormat("{0} - Crouch\n", GetKeyboardNegative(PlayerInput.Instance.Vertical.action));
            builder.AppendFormat("{0} - Jump\n", GetKeyboardBinding(PlayerInput.Instance.Jump.action));
            builder.AppendFormat("{0} - Fire Range Weapon\n", GetKeyboardBinding(PlayerInput.Instance.RangedAttack.action));
            builder.AppendFormat("{0} - Melee Attack\n", GetKeyboardBinding(PlayerInput.Instance.MeleeAttack.action));
            builder.AppendFormat("{0} - Pause Menu\n", GetKeyboardBinding(PlayerInput.Instance.Pause.action));

            textUI.SetText(builder);
        }

        private static string GetKeyboardBinding(InputAction action)
        {
            foreach (var binding in action.bindings)
            {
                if (binding.isComposite || binding.isPartOfComposite)
                    continue;

                if (binding.path.StartsWith("<Keyboard>"))
                    return FormatPath(binding.path);
            }
            return "Unbound";
        }

        private static string GetKeyboardPositive(InputAction action)
            => GetKeyboardCompositePart(action, "positive");

        private static string GetKeyboardNegative(InputAction action)
            => GetKeyboardCompositePart(action, "negative");

        private static string GetKeyboardCompositePart(InputAction action, string partName)
        {
            foreach (var binding in action.bindings)
            {
                if (!binding.isPartOfComposite)
                    continue;
                if (!string.Equals(binding.name, partName, System.StringComparison.OrdinalIgnoreCase))
                    continue;
                if (binding.path.StartsWith("<Keyboard>"))
                    return FormatPath(binding.path);
            }
            return "Unbound";
        }

        private static string FormatPath(string path)
        {
            int slash = path.LastIndexOf('/');
            string key = slash >= 0 ? path.Substring(slash + 1) : path;

            if (key.Length > 0)
                key = char.ToUpper(key[0]) + key.Substring(1);

            return key;
        }
    }
}