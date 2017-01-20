using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using MMOClient.Game;
using Protocol;

namespace MMOClient
{
	public partial class Form1 : Form
	{
		public int SizeX = 500;
		public int SizeY = 500;
		public int SectorSize = 20;

		private GameClient m_gameClient = new GameClient();
		private long m_lastTick = DateTime.Now.Ticks;

		public Form1()
		{
			InitializeComponent();
			SetStyle(ControlStyles.DoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint, true);

			timer1.Interval = 33;
			timer1.Enabled = true;
		}

		private void Form1_Paint(object sender, PaintEventArgs e)
		{
			for (int x = 0; x <= SizeX; x += SectorSize)
			{
				e.Graphics.DrawLine(new Pen(Color.Black), x, 0, x, SizeX);
			}

			for (int y = 0; y <= SizeY; y += SectorSize)
			{
				e.Graphics.DrawLine(new Pen(Color.Black), 0, y, SizeY, y);
			}

			int index = 0;
			foreach (PkGameObjectInfo gameObjectInfo in m_gameClient.GameObjectList)
			{
				if (index == 0)
				{
					e.Graphics.FillEllipse(new SolidBrush(Color.Green), gameObjectInfo.Pos.X - 2, gameObjectInfo.Pos.Z - 2, 5, 5);
				}
				else
				{
					e.Graphics.FillEllipse(new SolidBrush(Color.DodgerBlue), gameObjectInfo.Pos.X - 2, gameObjectInfo.Pos.Z - 2, 5, 5);
				}

				index++;
			}
		}

		private void timer1_Tick(object sender, System.EventArgs e)
		{
			long curTick = DateTime.Now.Ticks;

			m_gameClient.Update((float)(new TimeSpan(curTick - m_lastTick).TotalMilliseconds) / 1000 );

			m_lastTick = curTick;

			Invalidate();
		}

		private void Form1_Load(object sender, System.EventArgs e)
		{
			m_gameClient.LogAction = (s) =>
									 {
										 textBoxLog.AppendText(s + "\n");
									 };
		}

		private void Form1_MouseClick(object sender, MouseEventArgs e)
		{
			m_gameClient.Move(e.X, e.Y);
		}

		private void button1_Click(object sender, EventArgs e)
		{
			m_gameClient.Connect(int.Parse(textBox1.Text));
		}
	}
}
