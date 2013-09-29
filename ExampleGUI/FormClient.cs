using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using NamedPipeWrapper;

namespace ExampleGUI
{
    public partial class FormClient : Form
    {
        private readonly Client<string> _client = new Client<string>(Constants.PIPE_NAME);

        public FormClient()
        {
            InitializeComponent();
            Load += OnLoad;
        }

        private void OnLoad(object sender, EventArgs eventArgs)
        {
            _client.ServerMessage += OnServerMessage;
            _client.Start();
        }

        private void OnServerMessage(Connection<string> connection, string message)
        {
            richTextBoxMessages.Invoke(new Action(delegate
                {
                    AddLine("<b>Server</b>: " + message);
                }));
        }

        private void AddLine(string html)
        {
            richTextBoxMessages.Invoke(new Action(delegate
                {
                    richTextBoxMessages.Text += Environment.NewLine + "<div>" + html + "</div>";
                }));
        }

        private void buttonSend_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(textBoxMessage.Text))
                return;

            _client.PushMessage(textBoxMessage.Text);
            textBoxMessage.Text = "";
        }
    }
}
