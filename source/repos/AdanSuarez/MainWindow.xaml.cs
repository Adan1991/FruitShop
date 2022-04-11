using System.Windows;
using System.Data.SqlClient;
using System.Data;
using System.IO;
using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;

namespace AdanSuarez
{
	/// <summary>
	/// Lógica de interacción para MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		public MainWindow()
		{
			InitializeComponent();
		}

		private void Submit_Click(object sender, RoutedEventArgs e)
		{
			Dictionary<string, string> data = ReadFile();
			if (!(data is null) && data["PRODUCT"] == "PRICE") //I would put this strings in proect resources
			{
				InsertOrUpdateFruits(data);
			}
			else if (!(data is null) && data["PRODUCT"]=="QUANTITY")
			{
				GenerateReceipt(data);
			}
			else
			{
				errormessage.Text = "Error. The file provided should start by PRODUCT,PRICE or PRODUCT,QUANTITY in the first line";
			}

		}

		private void GenerateReceipt(Dictionary<string, string> data)
		{
			data = (Dictionary<string, string>)data.Where(x => x.Key != "PRODUCT");
			float price = 0;
			//The total price, the list of products purchased, the offers applied
			StringBuilder sbF = new StringBuilder();
			StringBuilder sbRecepit = new StringBuilder();
			//I normally would put this in the database
			sbF.Append(@"select Name,Price,ConditionSpent,ConditionQuantity,OfferDetail.ResultQuantity,ResultPrice,ResultQuantity,ResultFruitId from Fruit 
						inner join FruitOffers on FruitOffers.IdFruit= Fruit.Id 
						inner join OfferDetail on FruitOffers.IdOffer = OfferDetail.Id where Name in (");
			foreach(string fruit in data.Keys)
			{				
				sbF.Append(fruit + ",");
			}
			sbF.Remove(sbF.Length - 1, 1);
			sbF.Append(")");

			string connectionString = Properties.Settings.Default.AdanSuarezConnectionString.ToString();
			SqlConnection con = new SqlConnection(connectionString);
			con.Open();
			SqlCommand cmd = new SqlCommand(sbF.ToString(), con);
			cmd.CommandType = CommandType.Text;
			using (SqlDataReader reader = cmd.ExecuteReader())
			{
				if (reader.Read())
				{ //I normally would have separated the string building and the price calculations and use variables for better understanding, but I started this way because I'm running out of time.
					sbRecepit.AppendLine(reader["name"] + " : " + data[(string)reader["name"]] + "(" + reader["Price"] + ")");
					price= (float)reader["Price"] *Int32.Parse((data[(string)reader["name"]]));
					if (!(reader["conditionSpent"] is DBNull) && 
						(((float)reader["Price"]) * Int32.Parse((data[(string)reader["name"]])) >= (float)reader["conditionSpent"])
						||
						(reader["conditionQuantity"] is DBNull) &&
						(Int32.Parse((data[(string)reader["name"]])) >= (float)reader["conditionQuantity"])){
						if (!(reader["ResultPrice"] is DBNull)){
							price = price - (float)reader["ResultPrice"];
							sbRecepit.AppendLine("Offer for that product (-" + (float)reader["ResultPrice"] + ")");
						}
						if (!(reader["ResultQuantity"] is DBNull))						{
							sbRecepit.AppendLine("Offer for that product (a free fruit!" + reader["ResultFruitId"] + ")");
						}
					}
				}
				sbRecepit.AppendLine("----------");
				sbRecepit.AppendLine("TOTAL: " + price);

			}


		}

		private void InsertOrUpdateFruits(Dictionary<string, string> data)
		{
			try
			{
				data = (Dictionary<string, string>)data.Where(x => x.Key != "PRODUCT");
				StringBuilder sb = new StringBuilder();
				foreach (var fruit in data.Values)
				{
					string price = data[fruit];
					//I would put this in the database or change the wat parameters are inserted but I don't have enough time
					sb.Append(@"
						begin tran 
						if exists (select * from Fruit with (updlock,serializable) where Name = " + fruit + @")
						begin
						   update Fruit set Price = " + price + @"
						   where name = " + fruit + @"
						end
						else
						begin
						   insert into fruit (name, price)
						   values (" + fruit + ", " + price + @")
						end
						commit tran ");
				}
				string connectionString = Properties.Settings.Default.AdanSuarezConnectionString.ToString();
				SqlConnection con = new SqlConnection(connectionString);
				con.Open();
				SqlCommand cmd = new SqlCommand(sb.ToString(), con);
				cmd.CommandType = CommandType.Text;
				cmd.ExecuteNonQuery();
				con.Close();
				errormessage.Text = "Product prices readed";
			}
			catch (Exception ex)
			{
				errormessage.Text = "Error reading Product Prices";
			}


		}

		private Dictionary<string, string> ReadFile()
		{
			var ofd = new Microsoft.Win32.OpenFileDialog();
			var result = ofd.ShowDialog();
			if (result == false)
			{
				errormessage.Text = "Error reading the file.";
				return null;
			}
			TextBoxFile.Text = ofd.FileName;
			try
			{
				Dictionary<string,string> data = new Dictionary<string, string>();
				using (var reader = new StreamReader(ofd.FileName))
				{
					string line;
					while ((line = reader.ReadLine()) != null)
					{
						{
							string name = line.Split(',')[0];
							string number = line.Split(',')[1];
							data.Add(name, number);
						}
					}
				}
				return data;
			}
			catch (Exception ex)
			{
				errormessage.Text = "Error reading the file.";
				return null;
			}

		}
	}
}