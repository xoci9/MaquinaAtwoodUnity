using UnityEngine;

namespace VerletSimulation
{
    // Representa uma ligação (restrição) entre dois nós (Dots)
    public class Connection
    {
        // Primeiro nó da ligação
        public Dot dotA { get; }

        // Segundo nó da ligação
        public Dot dotB { get; }

        // Distância original que deve ser mantida entre os nós
        public float Length { get; }

        // Cria ligação com comprimento definido manualmente
        public Connection(Dot dotA, Dot dotB, float length)
        {
            this.dotA = dotA;
            this.dotB = dotB;
            Length = length;
        }

        // Cria ligação calculando o comprimento inicial a partir das posições
        public Connection(Dot dotA, Dot dotB)
        {
            this.dotA = dotA;
            this.dotB = dotB;
            Length = (dotA.Currentposition - dotB.Currentposition).magnitude;
        }

        // Retorna o nó oposto ao fornecido nesta ligação
        public Dot Other(Dot dot)
        {
            return (dot == dotA) ? dotB : dotA;
        }
    }
}
