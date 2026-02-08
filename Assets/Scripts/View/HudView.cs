using Mirror;
using UnityEngine;
using UnityEngine.UI;
using TDS.Ecs.Components;
using TMPro;

namespace TDS.View
{
    public class HudView : MonoBehaviour
    {
        public TMP_Text HealthText;
        public TMP_Text WeaponText;
        public TMP_Text AmmoText;

        private PlayerView _local;

        private void Update()
        {
            if (_local == null)
                TryResolveLocal();

            if (_local == null)
            {
                SetText(HealthText, "Health: --");
                SetText(WeaponText, "Weapon: --");
                SetText(AmmoText, "Ammo: --");
                return;
            }

            SetText(HealthText, $"Health: {_local.Health}");
            string weaponName = _local.WeaponType == WeaponType.None ? "Melee" : _local.WeaponType.ToString();
            SetText(WeaponText, $"Weapon: {weaponName}");
            string ammo = _local.WeaponType == WeaponType.None ? "-" : $"{_local.Ammo}/{_local.MagSize}";
            SetText(AmmoText, $"Ammo: {ammo}");
        }

        private void TryResolveLocal()
        {
            if (NetworkClient.localPlayer != null)
                _local = NetworkClient.localPlayer.GetComponent<PlayerView>();
        }

        private void SetText(TMP_Text text, string value)
        {
            if (text != null)
                text.text = value;
        }
    }
}
