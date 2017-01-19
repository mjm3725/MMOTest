using System;
using System.Collections.Generic;
using Protocol;
using SharpNav;



namespace MMOServer.Game
{
	class SectorManager
	{
		class Vector2Comparer : IComparer<Vector2>
		{
			public int Compare(Vector2 p1, Vector2 p2)
			{
				if (p1.X > p2.X)
				{
					return 1;
				}

				if (p1.X < p2.X)
				{
					return -1;
				}

				if (p1.Y > p2.Y)
				{

					return 1;
				}

				if (p1.Y < p2.Y)
				{
					return -1;
				}

				return 0;
			}
		}

		private readonly LinkedList<GameObject>[][] m_sector;
		private readonly int m_sectorSize;

		public SectorManager(int x, int z, int sectorSize)
		{
			m_sectorSize = sectorSize;

			int sizeX = (x % sectorSize == 0 ? x / sectorSize : x / sectorSize + 1);
			int sizeZ = (z % sectorSize == 0 ? z / sectorSize : z / sectorSize + 1);

			m_sector = new LinkedList<GameObject>[sizeX][];

			for (int i = 0; i < sizeX; i++)
			{
				m_sector[i] = new LinkedList<GameObject>[sizeZ];

				for (int j = 0; j < sizeZ; j++)
				{
					m_sector[i][j] = new LinkedList<GameObject>();
				}
			}
		}

		private bool CheckCoordinateValidation(int x, int z)
		{
			if (x < 0 || z < 0 || x >= m_sector.Length || z >= m_sector[x].Length)
			{
				return false;
			}

			return true;
		}

		private LinkedList<GameObject> GetSector(Vector3 position)
		{
			int x; 
			int z;
			GetSectorCoordinate(position, out x, out z);

			if (!CheckCoordinateValidation(x, z))
			{
				return null;
			}

			return m_sector[x][z];
		}

		private void VisitOne(int x, int z, Action<GameObject> action)
		{
			if (!CheckCoordinateValidation(x, z))
			{
				return;
			}

			foreach (GameObject gameObject in m_sector[x][z])
			{
				action(gameObject);
			}	
		}

		public SortedSet<Vector2> GetArroundSectorSet(int x, int z)
		{
			return new SortedSet<Vector2>(new Vector2Comparer())
					{
						new Vector2(x - 1, z - 1),
						new Vector2(x, z - 1),
						new Vector2(x + 1, z - 1),

						new Vector2(x - 1, z),
						new Vector2(x, z),
						new Vector2(x + 1, z),

						new Vector2(x - 1, z + 1),
						new Vector2(x, z + 1),
						new Vector2(x + 1, z + 1)
					};
		}

		public SortedSet<Vector2> GetArroundSectorSet(GameObject gameObject)
		{
			int x;
			int z;
			GetSectorCoordinate(gameObject, out x, out z);

			return GetArroundSectorSet(x, z);
		}

		public SortedSet<Vector2> GetArroundSectorSet(Vector3 position)
		{
			int x;
			int z;
			GetSectorCoordinate(position, out x, out z);

			return GetArroundSectorSet(x, z);
		}
		
		public int Enter(GameObject gameObject)
		{
			var sector = GetSector(gameObject.Position);

			if (sector == null)
			{
				return -1;
			}

			sector.AddLast(gameObject);

			return sector.Count;
		}

		public int Enter(int x, int z, GameObject gameObject)
		{
			if (!CheckCoordinateValidation(x, z))
			{
				return -1;
			}

			m_sector[x][z].AddLast(gameObject);

			return m_sector[x][z].Count;
		}

		public int Leave(GameObject gameObject)
		{
			var sector = GetSector(gameObject.Position);

			if (sector == null)
			{
				return -1;
			}

			sector.Remove(gameObject);

			return sector.Count;
		}

		public int Leave(int x, int z, GameObject gameObject)
		{
			if (!CheckCoordinateValidation(x, z))
			{
				return -1;
			}

			m_sector[x][z].Remove(gameObject);

			return m_sector[x][z].Count;
		}

		public void VisitMany(IEnumerable<Vector2> sectorList, Action<GameObject> action)
		{
			foreach (Vector2 p in sectorList)
			{
				VisitOne((int)p.X, (int)p.Y, action);
			}
		}

		public void VisitMany(IEnumerable<PkVector2> sectorList, Action<GameObject> action)
		{
			foreach (PkVector2 p in sectorList)
			{
				VisitOne((int)p.X, (int)p.Y, action);
			}
		}

		public void VisitArround(GameObject gameObject, Action<GameObject> action)
		{
			int x;
			int z;
			GetSectorCoordinate(gameObject, out x, out z);

            if (!CheckCoordinateValidation(x, z))
			{
				return;
			}

			VisitOne(x - 1, z - 1, action);
			VisitOne(x, z - 1, action);
			VisitOne(x + 1, z - 1, action);

			VisitOne(x - 1, z, action);
			VisitOne(x, z, action);
			VisitOne(x + 1, z, action);

			VisitOne(x - 1, z + 1, action);
			VisitOne(x, z + 1, action);
			VisitOne(x + 1, z + 1, action);
		}

		public void VisitArround(Vector3 position, Action<GameObject> action)
		{
			int x;
			int z;
			GetSectorCoordinate(position, out x, out z);

			if (!CheckCoordinateValidation(x, z))
			{
				return;
			}

			VisitOne(x - 1, z - 1, action);
			VisitOne(x, z - 1, action);
			VisitOne(x + 1, z - 1, action);

			VisitOne(x - 1, z, action);
			VisitOne(x, z, action);
			VisitOne(x + 1, z, action);

			VisitOne(x - 1, z + 1, action);
			VisitOne(x, z + 1, action);
			VisitOne(x + 1, z + 1, action);
		}

		public Vector2 GetSectorCoordinate(GameObject gameObject)
		{
			int x;
			int z;
			GetSectorCoordinate(gameObject, out x, out z);

			return new Vector2(x, z);
		}

		public Vector2 GetSectorCoordinate(Vector3 position)
		{
			int x;
			int z;
			GetSectorCoordinate(position, out x, out z);

			return new Vector2(x, z);
		}

		private void GetSectorCoordinate(GameObject gameObject, out int x, out int z)
		{
			x = (int)(gameObject.Position.X / m_sectorSize);
			z = (int)(gameObject.Position.Z / m_sectorSize);
		}

		private void GetSectorCoordinate(Vector3 position, out int x, out int z)
		{
			x = (int)(position.X / m_sectorSize);
			z = (int)(position.Z / m_sectorSize);
		}
				
        public void ClearSector(int x, int z, List<GameObject> removedObjList)
		{
            if (!CheckCoordinateValidation(x, z))
			{
				return;
			}

            removedObjList.AddRange(m_sector[x][z]);
			m_sector[x][z].Clear();
		}
	}
}
