﻿namespace SSharp.Tokens
{
    public class Block : ContainerToken
    {
        public bool IsDescendantOf(Block block)
        {
            if (block.Parent == null)
                return false;

            if (block == this)
                return true;

            if (Parent is Block parentBlock)
            {
                return parentBlock.IsDescendantOf(block);
            }
            else
            {
                return false;
            }
        }

        public override string ToString()
        {
            return "Block";
        }
    }
}
