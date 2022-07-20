using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum ChessPieceType
{
    None=0,
    Pawn=1,
    Rook=2,
    Knight=3,
    Bishop=4,
    Queen=5,
    King=6
}
public class chessPiece : MonoBehaviour
{
    public int team;
    public int currentX;
    public int currentY;
    public ChessPieceType type;
    public List<Vector2Int> AvailableMovesAI = new List<Vector2Int>();
    public SpecialMove1 specialMovesAI;

    private Vector3 desiredPosition;
    private Vector3 desiredScale = Vector3.one;

    private void Start()
    {
        if(team == 0)
        transform.rotation = Quaternion.Euler( new Vector3(-90, 0, 0));
    }
    private void Update()
    {
        transform.position = Vector3.Lerp(transform.position, desiredPosition, Time.deltaTime*10);
        transform.localScale = Vector3.Lerp(transform.localScale, desiredScale, Time.deltaTime * 10);
    }
    public virtual List<Vector2Int> GetAvailableMoves(ref chessPiece[,] board , int tileCountX, int tileCountY)
    {
        List<Vector2Int> r= new List<Vector2Int> ();
        r.Add(new Vector2Int(3, 3));
        r.Add(new Vector2Int(3, 4));
        r.Add(new Vector2Int(4, 3)); 
        r.Add(new Vector2Int(4, 4));


        return r;
    }
    public virtual SpecialMove1 GetSpecialMove1s (ref chessPiece[,] board,ref List<Vector2Int[]> movelist,ref List<Vector2Int> availableMoves)
    {
        return SpecialMove1.None;
    }
    public virtual void SetPosition( Vector3 position , bool force = false)
    {
        desiredPosition = position;
        if (force)
            transform.position = desiredPosition;
    }
    public virtual void SetScale( Vector3 scale , bool force = false)
    {
        desiredScale = scale;
        if (force)
            transform.localScale = desiredScale;
    }
}
