﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net;
using HAP = HtmlAgilityPack;
using System.Text.RegularExpressions;

namespace URLChecker
{
	public partial class frmMain : Form
	{
		private Timer timer = new Timer();
		private bool isRunning = false;
		private List<Model.Item> pageItems = new List<Model.Item>();
		private List<Model.Item> prevItems = new List<Model.Item>();
		
		public frmMain()
		{
			InitializeComponent();
		}

		private void textBox1_TextChanged(object sender, EventArgs e)
		{

		}

		private void btnRunToggle_Click(object sender, EventArgs e)
		{
			if (!isRunning)
			{
				ProcessPage();
				timer.Interval = int.Parse(txtInterval.Text) * 1000;
				timer.Tick += Timer_Tick;
				timer.Start();			

				btnRunToggle.Text = "Stop";
				isRunning = true;

			} else
			{
				timer.Stop();
				btnRunToggle.Text = "Start";
				isRunning = false;
			}
		}

		private void Timer_Tick(object sender, EventArgs e)
		{
			ProcessPage();	
		}

		private void ProcessPage()
		{
			tslblLastRefresh.Text = DateTime.Now.ToLongTimeString();
			tslblNextRefresh.Text = DateTime.Now.AddSeconds(int.Parse(txtInterval.Text)).ToLongTimeString();
			ParseItemsFromPage(txtUrl.Text);
		}

		private void ParseItemsFromPage(string url)
		{			
			var wc = new WebClient();			

			wc.DownloadStringAsync(new Uri(url));
			wc.DownloadProgressChanged += Wc_DownloadProgressChanged;
			wc.DownloadStringCompleted += Wc_DownloadStringCompleted;						
		}

		private void Wc_DownloadStringCompleted(object sender, DownloadStringCompletedEventArgs e)
		{
			prevItems = pageItems;
			pageItems.Clear();

			var regex = new Regex("\\$\\d*.\\d*");
			var html = e.Result;			

			var h = new HAP.HtmlDocument();
			h.LoadHtml(html);

			foreach (HAP.HtmlNode node in h.DocumentNode.Descendants().Where(n => n.HasClass("item-container")))
			{
				var item = new Model.Item();

				var pageItem = node?.Descendants().FirstOrDefault(i => i.HasClass("item-title"));
				item.Title = WebUtility.HtmlDecode(pageItem?.InnerText) ?? "";

				var button = node?.Descendants().FirstOrDefault(i => i.HasClass("item-button-area"));
				var btnText = button?.InnerText.Trim() ?? "";

				var promo = node?.Descendants().FirstOrDefault(i => i.HasClass("item-promo"));
				var stockText = node?.InnerText.Trim() ?? "";

				var priceLabel = node?.Descendants().FirstOrDefault(i => i.HasClass("price-current"));				
				item.Price = string.IsNullOrWhiteSpace(priceLabel?.InnerText) ? "" : regex.Match(priceLabel.InnerText).Value;

				item.Link = pageItem.GetAttributeValue("href", "") ?? "";

				item.NeweggId = item.Link.Split(new char[] { '/' }).Last();


				if (btnText.Contains("Add") || !stockText.Contains("OUT"))
				{
					item.InStock = true;
				}

				if ((item.InStock && chkInStock.Checked) || !chkInStock.Checked)					
				{
					if (!string.IsNullOrWhiteSpace(item.Price))
					{
						pageItems.Add(item);

						//CheckStockAlert(item);
					}
				}

				if (item.InStock)
				{
					for (int i = 0; i < 2; i++)
					{
						System.Media.SystemSounds.Beep.Play();
						System.Threading.Thread.Sleep(200);
					}
				}

				SortItems();
			}
			dgItemView.DataSource = null;
			dgItemView.DataSource = pageItems;			
		}

		private void CheckStockAlert(Model.Item item)
		{
			var prevItem = prevItems.Where(x => x.Link == item.Link).FirstOrDefault();

			if(!prevItem.InStock && item.InStock)
			{
				frmMain.ActiveForm.Activate();
				MessageBox.Show("ITEM IN STOCK!!!!!");
			}
		}

		private void SortItems()
		{
			if(cbSortBy.SelectedItem == null) { return; }
			switch (cbSortBy.SelectedItem.ToString())
			{
				case "Name":
					{
						pageItems = pageItems.OrderBy(x => x.Title).ToList();
						break;
					}
				case "Price":
					{
						pageItems = pageItems.OrderBy(x => x.Price).ToList();
						break;
					}
				default:
					return;
			}
		}

		private void Wc_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
		{
			tspbRefresh.Value = e.ProgressPercentage;
		}

		private void toolStripProgressBar1_Click(object sender, EventArgs e)
		{

		}

		private void frmMain_Load(object sender, EventArgs e)
		{
			dgItemView.CellFormatting += DgItemView_CellFormatting;
			dgItemView.CellClick += DgItemView_CellClick;			
		}
		

		private void DgItemView_CellClick(object sender, DataGridViewCellEventArgs e)
		{
			if (dgItemView.Columns[e.ColumnIndex].Name.Equals("Link"))
			{
				System.Diagnostics.Process.Start(dgItemView.Rows[e.RowIndex].Cells[e.ColumnIndex].Value.ToString());
			}

			if (dgItemView.Columns[e.ColumnIndex].Name.Equals("NeweggId"))
			{
				var link = "https://secure.newegg.com/Shopping/AddToCart.aspx?Submit=ADD&ItemList=" + dgItemView.Rows[e.RowIndex].Cells[e.ColumnIndex].Value.ToString();

				System.Diagnostics.Process.Start(link);
			}
		}

		private void DgItemView_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
		{
			if (dgItemView.Columns[e.ColumnIndex].Name.Equals("NeweggId"))
			{
				dgItemView.Columns[e.ColumnIndex].DefaultCellStyle.ForeColor = Color.Blue;
			}
			// Set the background to red for negative values in the Balance column.
			if (dgItemView.Columns[e.ColumnIndex].Name.Equals("InStock"))
			{
				if (bool.Parse(e.Value.ToString()) == true)
				{
					{
						dgItemView.Rows[e.RowIndex].DefaultCellStyle.BackColor = Color.LightGreen;						
					}
				}
			}
		}

		private void txtInterval_TextChanged(object sender, EventArgs e)
		{

		}
	}
}
