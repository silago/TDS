using Mirror;
using UnityEngine;
using UnityEngine.UI;

namespace TDS.View
{
    public class NetworkMenuView : MonoBehaviour
    {
        public InputField AddressInput;
        public InputField PortInput;
        public Button HostButton;
        public Button ClientButton;
        public Button ServerButton;
        public Button DisconnectButton;
        public Text StatusText;
        public GameObject PanelRoot;

        private NetworkManager _manager;
        private Transport _transport;

        private void Awake()
        {
            _manager = NetworkManager.singleton;
            _transport = Transport.active;

            if (HostButton != null) HostButton.onClick.AddListener(OnHost);
            if (ClientButton != null) ClientButton.onClick.AddListener(OnClient);
            if (ServerButton != null) ServerButton.onClick.AddListener(OnServer);
            if (DisconnectButton != null) DisconnectButton.onClick.AddListener(OnDisconnect);
        }

        private void Update()
        {
            if (StatusText != null)
                StatusText.text = GetStatus();

            bool connected = NetworkClient.isConnected || NetworkServer.active;
            if (PanelRoot != null)
                PanelRoot.SetActive(!connected);
            if (DisconnectButton != null)
                DisconnectButton.gameObject.SetActive(connected);
        }

        private void OnHost()
        {
            ApplyAddressAndPort();
            _manager.StartHost();
        }

        private void OnClient()
        {
            ApplyAddressAndPort();
            _manager.StartClient();
        }

        private void OnServer()
        {
            ApplyAddressAndPort();
            _manager.StartServer();
        }

        private void OnDisconnect()
        {
            if (NetworkServer.active && NetworkClient.isConnected)
                _manager.StopHost();
            else if (NetworkServer.active)
                _manager.StopServer();
            else if (NetworkClient.isConnected)
                _manager.StopClient();
        }

        private void ApplyAddressAndPort()
        {
            if (_manager == null)
                return;

            if (AddressInput != null && !string.IsNullOrWhiteSpace(AddressInput.text))
                _manager.networkAddress = AddressInput.text.Trim();

            if (PortInput != null && int.TryParse(PortInput.text, out int port))
                SetPort(port);
        }

        private void SetPort(int port)
        {
            if (_transport == null)
                return;

            var transportName = _transport.GetType().Name;
            if (transportName == "KcpTransport")
            {
                var kcp = (kcp2k.KcpTransport)_transport;
                kcp.Port = (ushort)Mathf.Clamp(port, 1, 65535);
            }
            else if (transportName == "TelepathyTransport")
            {
                var tel = (TelepathyTransport)_transport;
                tel.port = (ushort)Mathf.Clamp(port, 1, 65535);
            }
        }

        private string GetStatus()
        {
            if (NetworkServer.active && NetworkClient.isConnected)
                return "Host (Server + Client)";
            if (NetworkServer.active)
                return "Server";
            if (NetworkClient.isConnected)
                return "Client";
            return "Offline";
        }
    }
}
