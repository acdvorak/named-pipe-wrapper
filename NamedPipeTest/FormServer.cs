using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace NamedPipeTest
{
    public partial class FormServer : Form
    {
        private readonly UpdateServer _server = new UpdateServer();
        private readonly ISet<string> _clients = new HashSet<string>();

        public FormServer()
        {
            InitializeComponent();
            Load += OnLoad;
        }

        private void OnLoad(object sender, EventArgs eventArgs)
        {
            _server.ClientConnected += ServerOnClientConnected;
            _server.ClientDisconnected += ServerOnClientDisconnected;
            _server.ClientMessage += (client, message) => AddLine("<b>" + client.Name + "</b>: " + message);
        }

        private void ServerOnClientConnected(UpdateServerClient updateServerClient)
        {
            _clients.Add(updateServerClient.Name);
           AddLine("<b>" + updateServerClient.Name + "</b> connected!");
            UpdateClientList();
        }

        private void ServerOnClientDisconnected(UpdateServerClient updateServerClient)
        {
            _clients.Remove(updateServerClient.Name);
            AddLine("<b>" + updateServerClient.Name + "</b> disconnected!");
            UpdateClientList();
        }

        private void AddLine(string html)
        {
            richTextBoxMessages.Invoke(new Action(delegate
                {
                    richTextBoxMessages.Text += Environment.NewLine + "<div>" + html + "</div>";
                }));
        }

        private void UpdateClientList()
        {
            listBoxClients.Invoke(new Action(UpdateClientList2));
        }

        private void UpdateClientList2()
        {
            listBoxClients.Items.Clear();
            foreach (var client in _clients)
            {
                listBoxClients.Items.Add(client);
            }
        }

        private void buttonSend_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(textBoxMessage.Text))
                return;

            _server.PushMessage(textBoxMessage.Text);
            textBoxMessage.Text = "";
        }
    }
}
