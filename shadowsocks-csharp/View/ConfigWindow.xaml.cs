﻿using Shadowsocks.Controller;
using Shadowsocks.Encryption;
using Shadowsocks.Model;
using Shadowsocks.Util;
using Shadowsocks.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Shadowsocks.View
{
    public partial class ConfigWindow
    {
        public ConfigWindow(ShadowsocksController controller, int focusIndex)
        {
            InitializeComponent();
            SizeChanged += (o, args) => { GenQr(LinkTextBox.Text); };
            Splitter2.DragDelta += (o, args) => { GenQr(LinkTextBox.Text); };
            Closed += (o, e) => { _controller.ConfigChanged -= controller_ConfigChanged; };

            _controller = controller;
            foreach (var name in EncryptorFactory.GetEncryptor().Keys)
            {
                var info = EncryptorFactory.GetEncryptorInfo(name);
                if (info.display)
                {
                    EncryptionComboBox.Items.Add(name);
                }
            }
            foreach (var protocol in Protocols)
            {
                ProtocolComboBox.Items.Add(protocol);
            }
            foreach (var obfs in ObfsStrings)
            {
                ObfsComboBox.Items.Add(obfs);
            }

            _controller.ConfigChanged += controller_ConfigChanged;

            LoadCurrentConfiguration();
            if (focusIndex == -1)
            {
                var index = _modifiedConfiguration.index + 1;
                if (index < 0 || index > _modifiedConfiguration.configs.Count)
                    index = _modifiedConfiguration.configs.Count;

                focusIndex = index;
            }

            if (focusIndex >= 0 && focusIndex < _modifiedConfiguration.configs.Count)
            {
                SetServerListSelectedIndex(focusIndex);
            }
        }

        private static readonly string[] Protocols = {
                "origin",
                "verify_deflate",
                "auth_sha1_v4",
                "auth_aes128_md5",
                "auth_aes128_sha1",
                "auth_chain_a",
                "auth_chain_b",
                "auth_chain_c",
                "auth_chain_d",
                "auth_chain_e",
                "auth_chain_f",
                "auth_akarin_rand",
                "auth_akarin_spec_a"
        };

        private static readonly string[] ObfsStrings = {
                "plain",
                "http_simple",
                "http_post",
                "random_head",
                "tls1.2_ticket_auth",
                "tls1.2_ticket_fastauth"
        };

        private readonly ShadowsocksController _controller;

        private Configuration _modifiedConfiguration;

        public ServerViewModel ServerViewModel { get; set; } = new ServerViewModel();

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            LoadLanguage();
        }

        private void LoadLanguage()
        {
            Title = $@"{I18N.GetString(@"Edit Servers")}({(_controller.GetCurrentConfiguration().shareOverLan ? I18N.GetString(@"Any") : I18N.GetString(@"Local"))}:{_controller.GetCurrentConfiguration().localPort} {I18N.GetString(@"Version")}:{UpdateChecker.FullVersion})";

            foreach (var c in Utils.FindVisualChildren<Label>(this))
            {
                c.Content = I18N.GetString(c.Content.ToString());
            }

            foreach (var c in Utils.FindVisualChildren<Button>(this))
            {
                c.Content = I18N.GetString(c.Content.ToString());
            }

            foreach (var c in Utils.FindVisualChildren<CheckBox>(this))
            {
                c.Content = I18N.GetString(c.Content.ToString());
            }

            foreach (var c in Utils.FindVisualChildren<GroupBox>(this))
            {
                c.Header = I18N.GetString(c.Header.ToString());
            }

            TextBlock1.Text = I18N.GetString(TextBlock1.Text);
        }

        private void controller_ConfigChanged(object sender, EventArgs e)
        {
            LoadCurrentConfiguration();
        }

        private void LoadCurrentConfiguration()
        {
            _modifiedConfiguration = _controller.GetConfiguration();
            ServerViewModel.ReadConfig(_modifiedConfiguration);
            SetServerListSelectedIndex(_modifiedConfiguration.index);
        }

        public void SetServerListSelectedIndex(int index)
        {
            if (index < ServersListBox.Items.Count)
            {
                ServersListBox.SelectedIndex = index;
                ServersListBox.ScrollIntoView(ServersListBox.Items[index]);
            }
        }

        private void GenQr(string text)
        {
            try
            {
                var h = Convert.ToInt32(MainGrid.ActualHeight);
                var w = Convert.ToInt32(MainGrid.ColumnDefinitions[2].ActualWidth - PictureQrCode.Margin.Left - PictureQrCode.Margin.Right);
                PictureQrCode.Source = text != string.Empty
                        ? QrCodeUtils.GenQrCode(text, w, h)
                        : QrCodeUtils.GenQrCode2(text, Math.Min(w, h));
            }
            catch
            {
                PictureQrCode.Source = null;
            }
        }

        private void LinkTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            GenQr(LinkTextBox.Text);
        }

        private void ServersListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ServerGroupBox.Visibility = ServersListBox.SelectedIndex == -1 ? Visibility.Hidden : Visibility.Visible;
        }

        private void ObfsComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                var obfs = (Obfs.ObfsBase)Obfs.ObfsFactory.GetObfs(ObfsComboBox.SelectedItem.ToString());
                var properties = obfs.GetObfs()[ObfsComboBox.SelectedItem.ToString()];
                ObfsParamTextBox.IsEnabled = properties[2] > 0;
            }
            catch
            {
                ObfsParamTextBox.IsEnabled = true;
            }
        }

        private void LinkTextBox_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                var textBox = (TextBox)sender;
                textBox.Dispatcher.BeginInvoke(new Action(() => { textBox.SelectAll(); }));
            }
        }

        private void LinkTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            ((TextBox)sender).SelectAll();
        }

        private void SaveServers()
        {
            _modifiedConfiguration.configs.Clear();
            foreach (var serverObject in ServerViewModel.ServerCollection)
            {
                var server = new Server
                {
                    server = serverObject.ServerName,
                    server_port = serverObject.ServerPort,
                    server_udp_port = serverObject.ServerUdpPort,
                    password = serverObject.Password,
                    method = serverObject.Method,
                    protocol = serverObject.Protocol,
                    protocolparam = serverObject.ProtocolParam,
                    obfs = serverObject.ObfsName,
                    obfsparam = serverObject.ObfsParam,
                    remarks = serverObject.Remarks,
                    group = serverObject.Group,
                    udp_over_tcp = serverObject.UdpOverTcp,
                    id = serverObject.Id,

                    enable = serverObject.Enable
                };

                //Configuration.CheckServer(server);

                server.setProtocolData(serverObject.Protocoldata);
                server.setProtocolData(serverObject.Obfsdata);
                _modifiedConfiguration.configs.Add(server);
            }
        }

        private bool SaveConfig()
        {
            SaveServers();
            if (_modifiedConfiguration.configs.Count == 0)
            {
                MessageBox.Show(I18N.GetString(@"Please add at least one server"));
                return false;
            }

            _controller.SaveServersConfig(_modifiedConfiguration);
            return true;
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            if (SaveConfig())
            {
                Close();
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void ApplyButton_Click(object sender, RoutedEventArgs e)
        {
            if (SaveConfig())
            { }
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            if (ServersListBox.SelectedIndex == -1)
            {
                ServerViewModel.ServerCollection.Add(ServerObject.GetDefaultServer());
                SetServerListSelectedIndex(ServerViewModel.ServerCollection.Count - 1);
            }
            else
            {
                var position = ServersListBox.SelectedIndex + 1;
                ServerViewModel.ServerCollection.Insert(position, ServerObject.Clone((ServerObject)ServersListBox.SelectedItem));
                if (position <= _modifiedConfiguration.index)
                {
                    ++_modifiedConfiguration.index;
                }
                SetServerListSelectedIndex(position);
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            foreach (ServerObject selectedItem in ServersListBox.SelectedItems.Cast<object>().ToArray())
            {
                var position = ServerViewModel.ServerCollection.IndexOf(selectedItem);
                ServerViewModel.ServerCollection.Remove(selectedItem);
                if (position < _modifiedConfiguration.index)
                {
                    --_modifiedConfiguration.index;
                }
            }
            SetServerListSelectedIndex(_modifiedConfiguration.index);
        }

        private void UpButton_Click(object sender, RoutedEventArgs e)
        {
            var sortedCopy = new SortedDictionary<int, object>();
            foreach (ServerObject selectedItem in ServersListBox.SelectedItems.Cast<object>().ToArray())
            {
                sortedCopy.Add(ServerViewModel.ServerCollection.IndexOf(selectedItem), selectedItem);
            }
            foreach (var selectedItem in sortedCopy)
            {
                var position = selectedItem.Key;
                if (position > 0)
                {
                    ServerViewModel.ServerCollection.Move(position, position - 1);
                    if (position == _modifiedConfiguration.index + 1)
                    {
                        ++_modifiedConfiguration.index;
                    }
                    else if (position == _modifiedConfiguration.index)
                    {
                        --_modifiedConfiguration.index;
                    }
                }
                else
                {
                    break;
                }
            }

            foreach (var selectedItem in sortedCopy)
            {
                ServersListBox.SelectedItems.Add(selectedItem.Value);
            }

            if (ServersListBox.SelectedItem != null)
            {
                ServersListBox.ScrollIntoView(ServersListBox.SelectedItem);
            }
        }

        private void DownButton_Click(object sender, RoutedEventArgs e)
        {
            var sortedCopy = new SortedDictionary<int, object>();
            foreach (ServerObject selectedItem in ServersListBox.SelectedItems.Cast<object>().ToArray())
            {
                sortedCopy.Add(ServerViewModel.ServerCollection.IndexOf(selectedItem), selectedItem);
            }

            var reverseSortedCopy = sortedCopy.Reverse().ToArray();

            foreach (var selectedItem in reverseSortedCopy)
            {
                var position = selectedItem.Key;
                if (position + 1 < ServerViewModel.ServerCollection.Count)
                {
                    ServerViewModel.ServerCollection.Move(position, position + 1);
                    if (position == _modifiedConfiguration.index - 1)
                    {
                        --_modifiedConfiguration.index;
                    }
                    else if (position == _modifiedConfiguration.index)
                    {
                        ++_modifiedConfiguration.index;
                    }
                }
                else
                {
                    break;
                }
            }

            foreach (var selectedItem in reverseSortedCopy)
            {
                ServersListBox.SelectedItems.Add(selectedItem.Value);
            }

            if (ServersListBox.SelectedItem != null)
            {
                ServersListBox.ScrollIntoView(ServersListBox.SelectedItem);
            }
        }

    }
}
