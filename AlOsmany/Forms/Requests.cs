﻿using AlOsmanyDataModel;
using AlOsmanyDataModel.Models;
using System;
using System.Data.Entity;
using System.Linq;
using System.Threading;
using System.Windows.Forms;

namespace AlOsmany.Forms
{
    public partial class Requests : Form
    {
        private readonly User _userLoggedIn;
        private readonly AlOsmanyDbContext _alOsmanyDbContext;

        public Requests(User userLoggedIn)
        {
            InitializeComponent();

            _userLoggedIn = userLoggedIn;
            _alOsmanyDbContext = new AlOsmanyDbContext();

            if (_userLoggedIn.Role == UserRole.Customer)
            {
                dataGridView1.DataSource = _alOsmanyDbContext.Requests.Where(request => request.CreatedBy == _userLoggedIn.Id).ToListAsync().Result;
                button3.Visible = false;

                comboBox1.Items.AddRange(new[] {
                    "New",
                    "Assigned",
                    "InProgress",
                    "Completed"});
            }
            else if (_userLoggedIn.Role == UserRole.Worker)
            {
                dataGridView1.DataSource = _alOsmanyDbContext.Requests.Where(request => request.WorkerId == _userLoggedIn.Id).ToListAsync().Result;
                comboType.Enabled = true;

                comboBox1.Items.AddRange(new[] {
                    "Assigned",
                    "InProgress",
                    "Completed"});
            }
            else
            {
                dataGridView1.DataSource = _alOsmanyDbContext.Requests.ToListAsync().Result;
                txtWorker.ReadOnly = false;

                comboBox1.Items.AddRange(new[] {
                    "Assigned"});
            }

            comboBox1.SelectedIndex = 0;
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Close();

            var thread = new Thread(() => Application.Run(new Dashboard(_userLoggedIn)));
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
        }

        private async void button1_Click(object sender, EventArgs e)
        {
            Enum.TryParse(comboBox1.Text, out RequestType type);

            if (_userLoggedIn.Role == UserRole.Customer)
                dataGridView1.DataSource = await _alOsmanyDbContext.Requests.Where(request => request.CreatedBy == _userLoggedIn.Id && request.Type == type).ToListAsync();
            else if (_userLoggedIn.Role == UserRole.Worker)
                dataGridView1.DataSource = await _alOsmanyDbContext.Requests.Where(request => request.WorkerId == _userLoggedIn.Id && request.Type == type).ToListAsync();
            else
                dataGridView1.DataSource = await _alOsmanyDbContext.Requests.Where(request => request.Type == type).ToListAsync();

            dataGridView1.ClearSelection();

            txtId.Text = string.Empty;
            comboType.Items.Clear();
            txtWorker.Text = string.Empty;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            Clear();
        }

        private void dataGridView1_Click(object sender, EventArgs e)
        {
            if (dataGridView1.SelectedCells.Count != 0)
            {
                var id = (int)dataGridView1.CurrentRow.Cells[0].Value;

                var selectedRequest = _alOsmanyDbContext.Requests.Where(request => request.Id == id).FirstOrDefault();

                txtId.Text = selectedRequest.Id.ToString();

                if(_userLoggedIn.Role == UserRole.Worker)
                {
                    comboType.Items.Add(RequestType.Assigned);
                    comboType.Items.Add(RequestType.InProgress);
                    comboType.Items.Add(RequestType.Completed);
                }
                else
                    comboType.Items.Add(selectedRequest.Type);
                
                comboType.SelectedItem = selectedRequest.Type;

                var customer = _alOsmanyDbContext.Users.Where(user => user.Id == selectedRequest.CreatedBy).FirstOrDefault();
                var worker = _alOsmanyDbContext.Users.Where(user => user.Id == selectedRequest.WorkerId).FirstOrDefault();

                txtWorker.Text = worker is null ? string.Empty : worker.UserName;
            }
        }

        private async void Clear()
        {
            dataGridView1.ClearSelection();

            txtId.Text = string.Empty;
            comboType.Items.Clear();
            txtWorker.Text = string.Empty;

            if (_userLoggedIn.Role == UserRole.Customer)
                dataGridView1.DataSource = await _alOsmanyDbContext.Requests.Where(request => request.CreatedBy == _userLoggedIn.Id).ToListAsync();
            else if (_userLoggedIn.Role == UserRole.Worker)
                dataGridView1.DataSource = await _alOsmanyDbContext.Requests.Where(request => request.WorkerId == _userLoggedIn.Id).ToListAsync();
            else
                dataGridView1.DataSource = await _alOsmanyDbContext.Requests.ToListAsync();
        }

        private async void button3_Click(object sender, EventArgs e)
        {
            var id = (int)dataGridView1.CurrentRow.Cells[0].Value;
            var selectedRequest = _alOsmanyDbContext.Requests.Where(request => request.Id == id).FirstOrDefault();

            if (_userLoggedIn.Role == UserRole.Manager)
            {
                var workerUserName = txtWorker.Text;

                var worker = _alOsmanyDbContext.Users.Where(user => user.UserName == workerUserName).FirstOrDefault();

                if (worker is null)
                {
                    MessageBox.Show("Worker Not Found.", "Requests", MessageBoxButtons.RetryCancel, MessageBoxIcon.Error);
                    return;
                }

                selectedRequest.WorkerId = worker.Id;
                selectedRequest.Type = RequestType.Assigned;
            }
            else
            {
                Enum.TryParse(comboType.Text, out RequestType type);
                selectedRequest.Type = type;
            }

            await _alOsmanyDbContext.SaveChangesAsync();
            MessageBox.Show("Request updated.", "Requests");
            Clear();
        }
    }
}