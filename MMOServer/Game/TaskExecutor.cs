using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace MMOServer.Game
{
	public class TaskExecutor
	{
		private ConcurrentQueue<Action> m_actionQueue = new ConcurrentQueue<Action>();
		private long m_actionNum;


		public void PushAction(Action action)
		{
			long actionNum = Interlocked.Increment(ref m_actionNum);

			m_actionQueue.Enqueue(action);

			// 큐에 데이터가 처음들어와서 task실행해줘야하는 경우면 실행
			if (actionNum == 1)
			{
				RunActions();
			}
		}

		protected void RunActions()
		{
			Task.Run(() =>
			{
				long dequeueNum = 0;

				while (true)
				{
					Action dequeuedAction;

					if (m_actionQueue.TryDequeue(out dequeuedAction))
					{
						dequeuedAction();
						dequeueNum++;
					}
					else
					{
						break;
					}
				}

				// 남은 action이 있으면 Task 재호출
				long remainNum = Interlocked.Add(ref m_actionNum, -dequeueNum);

				if (remainNum > 0)
				{
					RunActions();
				}
			});
		}
	}
}
