using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace GarageGestion {
 public sealed class MainForm : Form {
  private readonly PictureBox companyLogo = new PictureBox();
  private readonly string logoPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "GarageGestion", "company-logo.png");

  public MainForm() {
   Text = "Garage Gestion";
   StartPosition = FormStartPosition.CenterScreen;
   MinimumSize = new Size(900, 600);
   Size = new Size(1100, 720);
   BackColor = Color.FromArgb(245,247,250);
   Font = new Font("Segoe UI", 9F);

   var header = new Panel { Dock=DockStyle.Top, Height=82, BackColor=Color.FromArgb(29,48,73) };
   companyLogo.Size = new Size(58,58);
   companyLogo.Location = new Point(14,12);
   companyLogo.SizeMode = PictureBoxSizeMode.Zoom;
   companyLogo.BackColor = Color.White;
   companyLogo.BorderStyle = BorderStyle.FixedSingle;
   header.Controls.Add(companyLogo);
   header.Controls.Add(new Label { Text="GARAGE GESTION", ForeColor=Color.White, Font=new Font("Segoe UI",19F,FontStyle.Bold), AutoSize=true, Location=new Point(86,14) });
   header.Controls.Add(new Label { Text="Licensed by Aymen AHMED ESSALAH", ForeColor=Color.Gainsboro, AutoSize=true, Location=new Point(88,49) });
   var logoButton = new Button { Text="Logo de mon entreprise", Width=190, Height=32, Anchor=AnchorStyles.Top|AnchorStyles.Right, Location=new Point(890,24) };
   logoButton.Click += delegate { ChooseCompanyLogo(); };
   header.Controls.Add(logoButton);
   header.Resize += delegate { logoButton.Left = header.ClientSize.Width - logoButton.Width - 18; };

   var menu = new FlowLayoutPanel { Dock=DockStyle.Left, Width=220, Padding=new Padding(14), FlowDirection=FlowDirection.TopDown, WrapContents=false, BackColor=Color.FromArgb(235,239,244) };
   string[] entries = { "Tableau de bord","Clients","Véhicules","Ordres de réparation","Devis","Factures","Fournisseurs","Pièces et stock","Paiements","Documents PDF" };
   foreach (var entry in entries) {
    var b = new Button { Text=entry, Width=185, Height=38, TextAlign=ContentAlignment.MiddleLeft, FlatStyle=FlatStyle.Flat, BackColor=Color.White, Margin=new Padding(0,0,0,8), Padding=new Padding(10,0,0,0) };
    b.Click += delegate { MessageBox.Show("Module en préparation : " + entry, "Garage Gestion"); };
    menu.Controls.Add(b);
   }
   var content = new Panel { Dock=DockStyle.Fill, Padding=new Padding(30) };
   content.Controls.Add(new Label { Text="Bienvenue dans Garage Gestion", Font=new Font("Segoe UI",22F,FontStyle.Bold), ForeColor=Color.FromArgb(29,48,73), AutoSize=true, Location=new Point(30,35) });
   content.Controls.Add(new Label { Text="Votre espace de gestion de garage.\r\n\r\nConfigurez le logo de votre entreprise avec le bouton en haut à droite.\r\nLes modules métier seront activés progressivement.", Font=new Font("Segoe UI",11F), ForeColor=Color.DimGray, AutoSize=true, Location=new Point(34,95) });
   Controls.Add(content);
   Controls.Add(menu);
   Controls.Add(header);
   LoadCompanyLogo();
   Shown += FirstRunLogoPrompt;
  }

  private void FirstRunLogoPrompt(object sender, EventArgs e) {
   if (File.Exists(logoPath)) return;
   if (MessageBox.Show("Voulez-vous choisir le logo de votre garage maintenant ?", "Configuration de Garage Gestion", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
    ChooseCompanyLogo();
  }

  private void ChooseCompanyLogo() {
   using (var dialog = new OpenFileDialog()) {
    dialog.Title = "Choisir le logo de votre entreprise";
    dialog.Filter = "Images|*.png;*.jpg;*.jpeg;*.bmp|Tous les fichiers|*.*";
    if (dialog.ShowDialog(this) != DialogResult.OK) return;
    try {
     string folder = Path.GetDirectoryName(logoPath);
     if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
     using (var source = Image.FromFile(dialog.FileName))
      using (var copy = new Bitmap(source))
       copy.Save(logoPath, System.Drawing.Imaging.ImageFormat.Png);
     LoadCompanyLogo();
    } catch (Exception ex) {
     MessageBox.Show("Impossible d'enregistrer ce logo : " + ex.Message, "Garage Gestion", MessageBoxButtons.OK, MessageBoxIcon.Error);
    }
   }
  }

  private void LoadCompanyLogo() {
   if (!File.Exists(logoPath)) return;
   try {
    Image old = companyLogo.Image;
    using (var stream = new FileStream(logoPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)) {
     companyLogo.Image = Image.FromStream(stream);
     companyLogo.Image = new Bitmap(companyLogo.Image);
    }
    if (old != null) old.Dispose();
   } catch { }
  }
 }
}