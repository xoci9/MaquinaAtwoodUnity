using System.Collections.Generic;
using UnityEngine;

namespace VerletSimulation
{
    // Representa um nó móvel na simulação Verlet
    public class Dot
    {
        // Posição atual do nó
        public Vector3 Currentposition { get; set; }

        // Posição do nó no frame anterior
        public Vector3 LastPosition { get; set; }

        // Indica se o nó está fixo no espaço (não se move)
        public bool isLocked { get; set; }

        // Peso do nó usado no cálculo de acelerações
        public float mass { get; set; }

        // Opcional: objeto Unity para representar visualmente este nó
        public GameObject gameObject;

        // Conexões (restrições) deste nó com outros
        public List<Connection> connections { get; } = new List<Connection>();

        // Construtor principal: define posição, estado fixo e massa
        public Dot(Vector3 initialPosition, bool isLocked, float mass)
        {
            Currentposition = initialPosition;
            LastPosition = initialPosition;
            this.isLocked = isLocked;
            this.mass = Mathf.Max(0.01f, mass); // Garante massa mínima para não dividir por zero
        }

        // Construtor secundário: massa padrão igual a 1
        public Dot(Vector3 initialPosition, bool isLocked)
            : this(initialPosition, isLocked, 1f)
        {
        }

        // Cria ligação entre dois nós, com comprimento opcional
        public static Connection Connect(Dot dotA, Dot dotB, float length = -1f)
        {
            Connection connection = (length < 0f)
                ? new Connection(dotA, dotB)
                : new Connection(dotA, dotB, length);

            dotA.connections.Add(connection);
            dotB.connections.Add(connection);
            return connection;
        }

        // Remove uma ligação existente entre dois nós
        public static void Disconnect(Connection connection)
        {
            if (connection.dotA.connections.Contains(connection))
                connection.dotA.connections.Remove(connection);

            if (connection.dotB.connections.Contains(connection))
                connection.dotB.connections.Remove(connection);
        }
    }
}
