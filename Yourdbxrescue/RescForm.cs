using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Threading;
using System.IO;
using System.Diagnostics;

namespace Yourdbxrescue {
    public partial class RescForm : Form {
        public RescForm() {
            InitializeComponent();
        }

        UtExplodeDbx.Stat3 s3last = null;

        int fno = 1;

        SynchronizationContext Sync;

        private void llVisit_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e) {
            Process.Start("http://www.geocities.co.jp/SiliconValley-Oakland/3664/");
        }

        private void RescForm_Load(object sender, EventArgs e) {
            Show();

            OpenFileDialog ofddbx = new OpenFileDialog();
            ofddbx.Filter = "*.dbx|*.dbx";
            ofddbx.DefaultExt = "dbx";
            if (ofddbx.ShowDialog(this) != DialogResult.OK) { Close(); return; }

            FolderBrowserDialog fbdRescExp = new FolderBrowserDialog();
            while (true) {
                fbdRescExp.Description = "救出したEMLファイルを保存するフォルダ？";
                if (fbdRescExp.ShowDialog(this) != DialogResult.OK) { Close(); return; }

                int n = Directory.GetFiles(fbdRescExp.SelectedPath).Length;
                if (n == 0) break;

                DialogResult dr = MessageBox.Show(this, String.Format("次のフォルダには {0:#,##0} 個のファイルが有ります。\n\n" + fbdRescExp.SelectedPath + "\n\n" + "他のファイルと混ざってしまう可能性が有ります。\n\n" + "本当に続行しますか。", n), null, MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation);
                if (dr == DialogResult.Yes) break;
                if (dr != DialogResult.No) { Close(); return; }
            }

            String fpdbx = ofddbx.FileName;
            String dir = fbdRescExp.SelectedPath;

            bwResc.DoWork += delegate(object sender2, DoWorkEventArgs e2) {
                UtExplodeDbx.ExplodeDbx3(fpdbx,
                    delegate(UtExplodeDbx.Stat3 s3) {
                        Sync.Send(delegate {
                            if (bwResc.CancellationPending) throw new ApplicationException();
                            s3last = s3;
                            {
                                l1.Text = String.Format("{0:0}%", s3last.x);
                                l2.Text = String.Format("{0:0}% -- {1:#,##0}個", s3last.z, s3last.emailcnt);
                                pb1.Value = s3last.x;
                                pb2.Value = s3last.z;
                                s3last = null;
                            }
                        }, null);
                    },
                    delegate() {
                        for (int x = 0; ; x++) {
                            if (bwResc.CancellationPending) throw new ApplicationException();
                            String fp = Path.Combine(dir, String.Format("{0:000000}.eml", fno++));
                            if (!File.Exists(fp))
                                return File.Create(fp);
                        }
                    }
                    );
            };

            Sync = SynchronizationContext.Current;
            bwResc.RunWorkerAsync();
        }

        private void timer1_Tick(object sender, EventArgs e) {
            if (s3last != null) {
                l1.Text = String.Format("{0:0}%", s3last.x);
                l2.Text = String.Format("{0:0}% {1:#,##0}個", s3last.z, s3last.emailcnt);
                pb1.Value = s3last.x;
                pb2.Value = s3last.z;
                s3last = null;
            }
        }

        private void RescForm_FormClosing(object sender, FormClosingEventArgs e) {
            if (bwResc.IsBusy ) {
                if (MessageBox.Show(this, "処理を中止して、終了します。", null, MessageBoxButtons.OKCancel, MessageBoxIcon.Exclamation) == DialogResult.OK) {
                    bwResc.CancelAsync();
                }
                else {
                    e.Cancel = true;
                }
            }
        }

        private void bwResc_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e) {
            if (e.Error == null) {
                flpOk.Show();
            }
            else {
                llErr.Tag = e.Error;
                flpFail.Show();
            }
        }

        private void llErr_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e) {
            MessageBox.Show(this, "" + llErr.Tag, null, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
        }
    }
}