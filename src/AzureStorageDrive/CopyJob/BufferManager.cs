using AzureStorageDrive.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AzureStorageDrive.CopyJob
{
    public class BufferManager
    {
        public byte[] Bytes { get; set; }

        private Queue<int> availBlockIds = new Queue<int>();
        private object availBlockLock = new object();
        private AutoResetEvent availBlockEvent = new AutoResetEvent(false);

        public BufferManager()
        {
            Bytes = new byte[Constants.BlockSize * Constants.Parallalism];
            for (var i = 0; i < Constants.Parallalism; ++i)
            {
                availBlockIds.Enqueue(i);
            }

        }

        public int GetTotalBlockCount()
        {
            return Constants.Parallalism;
        }

        public List<int> ReserveBlocksBySize(long size)
        {
            var count = this.CalculateMaxBlockCount(size);
            return ReserveBlocks(count);
        }

        public List<int> ReserveBlocks(int maxCount = 1) {
            var blockIds = new List<int>();

            //if the target is an empty file or directory
            if (maxCount <= 0)
            {
                return blockIds;
            }

            if (availBlockIds.Count > 0)
            {
                lock (availBlockLock)
                {
                    if (availBlockIds.Count > 0)
                    {
                        for (var i = 0; i < maxCount && i < availBlockIds.Count; ++i)
                        {
                            blockIds.Add(availBlockIds.Dequeue());
                        }

                        return blockIds;
                    }

                    availBlockEvent.Reset();
                }
            }

            //not found a available block
            availBlockEvent.WaitOne();
            return ReserveBlocks(maxCount);
        }

        public void ReleaseBlock(params int[] blockIds)
        {
            lock (availBlockLock)
            {
                foreach(var id in blockIds)
                {
                    availBlockIds.Enqueue(id);
                }
            }

            availBlockEvent.Set();
        }

        public BufferRange GetRange(int blockId)
        {
            return new BufferRange()
            {
                Start = blockId * Constants.BlockSize,
                Length = Constants.BlockSize
            };
        }

        public int CalculateMaxBlockCount(long size)
        {
            if (size <= 0)
            {
                return 0;
            }

            var blocks = (int)(size / Constants.BlockSize);
            if (blocks == 0 && size > 0)
            {
                return 1;
            }

            if (size % Constants.BlockSize > 0)
            {
                blocks++;
            }

            return blocks;
        }
    }
}
