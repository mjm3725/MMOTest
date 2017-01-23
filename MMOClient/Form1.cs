using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using MMOClient.Game;
using Protocol;
using System.Threading;
using System.Threading.Tasks;

namespace MMOClient
{
	public partial class Form1 : Form
	{
		public int SizeX = 500;
		public int SizeY = 500;
		public int SectorSize = 20;

		private List<GameClient> m_gameClients = new List<GameClient>();
		private long m_lastTick = DateTime.Now.Ticks;
		private Random m_rand = new Random((int)DateTime.Now.Ticks);

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

			if (m_gameClients.Count == 1)
			{
				foreach (PkGameObjectInfo gameObjectInfo in m_gameClients[0].GameObjectList)
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
			else
			{
				foreach(var c in m_gameClients)
				{
					if (c.GameObjectList.Count > 0)
					{
						e.Graphics.FillEllipse(new SolidBrush(Color.Green), c.GameObjectList[0].Pos.X - 2, c.GameObjectList[0].Pos.Z - 2, 5, 5);
					}
				}
			}
		}

		private void timer1_Tick(object sender, System.EventArgs e)
		{
			Task.Run(() =>
			{
				long curTick = DateTime.Now.Ticks;

				Parallel.ForEach(m_gameClients, (c) =>
				{
					c.Update((float)(new TimeSpan(curTick - m_lastTick).TotalMilliseconds) / 1000);
				});

				m_lastTick = curTick;

				Invalidate();
			});
		}

		private void Form1_Load(object sender, System.EventArgs e)
		{

		}

		private void Form1_MouseClick(object sender, MouseEventArgs e)
		{
			if (m_gameClients.Count > 1)
			{
				Parallel.ForEach(m_gameClients, (c) =>
				{
					if (c.GameObjectList.Count > 0 && (c.GameObjectList[0].MoveInfo == null || c.GameObjectList[0].MoveInfo.MoveState == 0))
					{
						c.Move(m_rand.Next(50, 200), m_rand.Next(50, 200));
					}
				});
			}
			else if (m_gameClients.Count == 1)
			{
				m_gameClients[0].Move(e.X, e.Y);
			}
		}

		private void button1_Click(object sender, EventArgs e)
		{
			int num = int.Parse(textBoxNum.Text);

			for (int i = 0; i < num; i++)
			{
				m_gameClients.Add(new GameClient());
			}
						
			Parallel.ForEach(m_gameClients, (c) =>
			{
				c.LogAction = (s) =>
				{
					//if (textBoxLog.IsAccessible)
					//{
					//	textBoxLog.AppendText(s + "\n");
					//}
					//else
					//{
					//	textBoxLog.Invoke((MethodInvoker)(() => { textBoxLog.AppendText(s + "\n"); }));
					//}
				};

				c.Connect(int.Parse(textBoxPort.Text));
			});
		}
	}
}
