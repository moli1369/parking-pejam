using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace mpas_pejam
{
    public partial class Form1 : Form
    {
        /// <summary>true = occupied (red), false = free (green)</summary>
        private readonly Dictionary<string, bool> _occupied = new Dictionary<string, bool>();

        private static readonly string[] SpotNames =
        {
            "panel1", "panel2", "panel3", "panel4", "panel5", "panel6", "panel7", "panel8", "panel9",
            "panel10", "panel11", "panel12", "panel13", "panel14", "panel16", "panel17", "panel18", "panel19",
            "panel21", "panel22", "panel23", "panel24", "panel26", "panel27", "panel28", "panel29",
            "panel31", "panel32", "panel33", "panel34", "panel36", "panel37", "panel38", "panel39",
            "panel41", "panel42", "panel470"
        };

        private static readonly Color ColorFree = Color.DarkGreen;
        private static readonly Color ColorOccupied = Color.Red;

        private readonly string _statePath;

        public Form1()
        {
            InitializeComponent();

            _statePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "parking-pejam",
                "spots.txt");

            WireSpots();
            LoadState();
            ApplyAllColors();
            UpdateTitleStats();

            FormClosing += Form1_FormClosing;
        }

        private IEnumerable<Panel> GetSpotPanels()
        {
            foreach (string name in SpotNames)
            {
                Control[] found = Controls.Find(name, true);
                if (found.Length > 0 && found[0] is Panel panel)
                    yield return panel;
            }
        }

        private void WireSpots()
        {
            foreach (Panel panel in GetSpotPanels())
            {
                if (!_occupied.ContainsKey(panel.Name))
                    _occupied[panel.Name] = true; // default occupied

                panel.Cursor = Cursors.Hand;
                panel.Click -= Spot_Click;
                panel.Click += Spot_Click;

                // Tooltip: spot id + status
                var tip = new ToolTip();
                tip.SetToolTip(panel, panel.Name);
            }
        }

        private void Spot_Click(object sender, EventArgs e)
        {
            var panel = sender as Panel;
            if (panel == null) return;

            bool current;
            if (!_occupied.TryGetValue(panel.Name, out current))
                current = true;

            _occupied[panel.Name] = !current;
            ApplyColor(panel);
            UpdateTitleStats();
            SaveState();
        }

        private void ApplyColor(Panel panel)
        {
            if (panel == null) return;
            bool occupied;
            if (!_occupied.TryGetValue(panel.Name, out occupied))
                occupied = true;

            panel.BackColor = occupied ? ColorOccupied : ColorFree;
        }

        private void ApplyAllColors()
        {
            foreach (Panel panel in GetSpotPanels())
                ApplyColor(panel);
        }

        private void UpdateTitleStats()
        {
            int total = _occupied.Count;
            int free = _occupied.Values.Count(v => !v);
            int occ = total - free;
            Text = string.Format("Parking Pejam  |  Free: {0}  |  Occupied: {1}  |  Total: {2}", free, occ, total);
        }

        private void LoadState()
        {
            try
            {
                if (!File.Exists(_statePath)) return;

                foreach (string line in File.ReadAllLines(_statePath))
                {
                    string[] parts = line.Split('=');
                    if (parts.Length != 2) continue;
                    string name = parts[0].Trim();
                    string val = parts[1].Trim();
                    if (SpotNames.Contains(name))
                        _occupied[name] = val == "1" || val.Equals("true", StringComparison.OrdinalIgnoreCase);
                }
            }
            catch
            {
                // ignore corrupt state; keep defaults
            }
        }

        private void SaveState()
        {
            try
            {
                string dir = Path.GetDirectoryName(_statePath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                var lines = new List<string>();
                foreach (var kv in _occupied.OrderBy(k => k.Key))
                    lines.Add(kv.Key + "=" + (kv.Value ? "1" : "0"));

                File.WriteAllLines(_statePath, lines);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Could not save parking state:\n" + ex.Message, "Parking Pejam",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            SaveState();
        }

        // --- Paint handlers kept for Designer compatibility; colors come from state ---

        private void PaintSpot(Panel panel)
        {
            ApplyColor(panel);
        }

        private void panel1_Paint(object sender, PaintEventArgs e) { PaintSpot(panel1); }
        private void panel2_Paint(object sender, PaintEventArgs e) { PaintSpot(panel2); }
        private void panel3_Paint(object sender, PaintEventArgs e) { PaintSpot(panel3); }
        private void panel4_Paint(object sender, PaintEventArgs e) { PaintSpot(panel4); }
        private void panel5_Paint(object sender, PaintEventArgs e) { PaintSpot(panel5); }
        private void panel6_Paint(object sender, PaintEventArgs e) { PaintSpot(panel6); }
        private void panel7_Paint(object sender, PaintEventArgs e) { PaintSpot(panel7); }
        private void panel8_Paint(object sender, PaintEventArgs e) { PaintSpot(panel8); }
        private void panel9_Paint(object sender, PaintEventArgs e) { PaintSpot(panel9); }
        private void panel10_Paint(object sender, PaintEventArgs e) { PaintSpot(panel10); }
        private void panel11_Paint(object sender, PaintEventArgs e) { PaintSpot(panel11); }
        private void panel12_Paint(object sender, PaintEventArgs e) { PaintSpot(panel12); }
        private void panel13_Paint(object sender, PaintEventArgs e) { PaintSpot(panel13); }
        private void panel14_Paint(object sender, PaintEventArgs e) { PaintSpot(panel14); }
        private void panel16_Paint(object sender, PaintEventArgs e) { PaintSpot(panel16); }
        private void panel17_Paint(object sender, PaintEventArgs e) { PaintSpot(panel17); }
        private void panel18_Paint(object sender, PaintEventArgs e) { PaintSpot(panel18); }
        private void panel19_Paint(object sender, PaintEventArgs e) { PaintSpot(panel19); }
        private void panel21_Paint(object sender, PaintEventArgs e) { PaintSpot(panel21); }
        private void panel22_Paint(object sender, PaintEventArgs e) { PaintSpot(panel22); }
        private void panel23_Paint(object sender, PaintEventArgs e) { PaintSpot(panel23); }
        private void panel24_Paint(object sender, PaintEventArgs e) { PaintSpot(panel24); }
        private void panel26_Paint(object sender, PaintEventArgs e) { PaintSpot(panel26); }
        private void panel27_Paint(object sender, PaintEventArgs e) { PaintSpot(panel27); }
        private void panel28_Paint(object sender, PaintEventArgs e) { PaintSpot(panel28); }
        private void panel29_Paint(object sender, PaintEventArgs e) { PaintSpot(panel29); }
        private void panel31_Paint(object sender, PaintEventArgs e) { PaintSpot(panel31); }
        private void panel32_Paint(object sender, PaintEventArgs e) { PaintSpot(panel32); }
        private void panel33_Paint(object sender, PaintEventArgs e) { PaintSpot(panel33); }
        private void panel34_Paint(object sender, PaintEventArgs e) { PaintSpot(panel34); }
        private void panel36_Paint(object sender, PaintEventArgs e) { PaintSpot(panel36); }
        private void panel37_Paint(object sender, PaintEventArgs e) { PaintSpot(panel37); }
        private void panel38_Paint(object sender, PaintEventArgs e) { PaintSpot(panel38); }
        private void panel39_Paint(object sender, PaintEventArgs e) { PaintSpot(panel39); }
        private void panel41_Paint(object sender, PaintEventArgs e) { PaintSpot(panel41); }
        private void panel42_Paint(object sender, PaintEventArgs e) { PaintSpot(panel42); }
        private void panel470_Paint(object sender, PaintEventArgs e) { PaintSpot(panel470); }

        // leftover Designer stubs (empty)
        private void label1_Click(object sender, EventArgs e) { }
        private void groupBox2_Enter(object sender, EventArgs e) { }
        private void label2_Click(object sender, EventArgs e) { }
        private void menuStrip1_ItemClicked(object sender, ToolStripItemClickedEventArgs e) { }
    }
}
