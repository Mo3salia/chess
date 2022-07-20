using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class pawn : chessPiece
{
   public override List<Vector2Int> GetAvailableMoves(ref chessPiece[,] board, int tileCountX, int tileCountY)
    {
        List<Vector2Int> r = new List<Vector2Int>();

        int direction = (team == 0) ?1 : -1;

        //one in front
        
        if(board[currentX, currentY + direction] == null)
            r.Add (new Vector2Int (currentX, currentY +direction));

        //two in front 
        if( board[currentX, currentY + direction] == null)
        {
            //white team 
            if(team==0 && currentY ==1 && board [currentX,currentY+(direction*2)] == null)
                r.Add(new Vector2Int (currentX,currentY+(direction*2)));
            //black team 
            if (team == 1 && currentY == 6 && board[currentX, currentY + (direction * 2)] == null)
                r.Add(new Vector2Int(currentX, currentY + (direction * 2)));
        }
        // kill move 
        if (currentX != tileCountX-1)
            if(board[currentX+1,currentY+direction] != null && board [currentX+1,currentY+direction].team != team)
                r.Add (new Vector2Int (currentX+1, currentY+direction));
        if (currentX != 0)
            if (board[currentX - 1, currentY + direction] != null && board[currentX - 1, currentY + direction].team != team)
                r.Add(new Vector2Int(currentX - 1, currentY + direction));
        return r;
    }
   
    public override  SpecialMove1 GetSpecialMove1s(ref chessPiece[,] board, ref List<Vector2Int[]> movelist, ref List<Vector2Int> availableMoves)
    {
        int direction = (team == 0) ? 1 : -1;

        //queening
        if ((team == 0 && currentY == 6) || (team == 1 && currentY == 1))
            return SpecialMove1.Promotion;

        //En passant 
        if(movelist.Count >0)
        {
            Vector2Int[] lastMove = movelist[movelist.Count - 1];
            if(board[lastMove[1].x,lastMove[1].y].type == ChessPieceType.Pawn) // if last piece moved was a pawn
            {
                if(Mathf.Abs(lastMove[0].y - lastMove[1].y) == 2) // if the last move was a +2 in either direction
                {
                    if(board[lastMove[1].x,lastMove[1].y].team  != team) // if the move was from the other team 
                    {
                        if(lastMove[1].y== currentY) //if both pawns are on the same y
                        {
                            if(lastMove[1].x == currentX-1) // landed leeft
                            {
                                availableMoves.Add(new Vector2Int(currentX - 1, currentY + direction));
                                return SpecialMove1.EnPassant;
                            }
                            else if (lastMove[1].x == currentX+1)
                            {
                                availableMoves.Add(new Vector2Int(currentX + 1, currentY + direction));
                                return SpecialMove1.EnPassant;
                            }

                        }
                    }
                }
            }
        }
            



        return SpecialMove1.None;
    }
}
