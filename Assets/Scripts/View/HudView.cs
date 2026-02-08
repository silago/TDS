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

        private void OnEnable()
        {
            PlayerView.LocalPlayerReady += OnLocalReady;
            TryResolveLocal();
        }

        private void OnDisable()
        {
            PlayerView.LocalPlayerReady -= OnLocalReady;
            Unsubscribe();
        }

        private void OnLocalReady(PlayerView view)
        {
            Bind(view);
        }

        private void TryResolveLocal()
        {
            if (NetworkClient.localPlayer != null)
                Bind(NetworkClient.localPlayer.GetComponent<PlayerView>());
        }

        private void Bind(PlayerView view)
        {
            if (view == null || view == _local)
                return;

            Unsubscribe();
            _local = view;
            _local.HealthChanged += OnHealthChanged;
            _local.WeaponChanged += OnWeaponChanged;

            OnHealthChanged(_local.Health);
            OnWeaponChanged(_local.WeaponType, _local.Ammo, _local.MagSize);
        }

        private void Unsubscribe()
        {
            if (_local == null)
                return;

            _local.HealthChanged -= OnHealthChanged;
            _local.WeaponChanged -= OnWeaponChanged;
            _local = null;
        }

        private void OnHealthChanged(int value)
        {
            SetText(HealthText, $"Health: {value}");
        }

        private void OnWeaponChanged(WeaponType type, int ammo, int magSize)
        {
            string weaponName = type == WeaponType.None ? "Melee" : type.ToString();
            SetText(WeaponText, $"Weapon: {weaponName}");
            string ammoText = type == WeaponType.None ? "-" : $"{ammo}/{magSize}";
            SetText(AmmoText, $"Ammo: {ammoText}");
        }

        private void SetText(TMP_Text text, string value)
        {
            if (text != null)
                text.text = value;
        }
    }
}
