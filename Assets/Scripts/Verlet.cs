using System.Collections.Generic;
using UnityEngine;

namespace VerletSimulation
{
    // Controla a simulação Verlet de um conjunto de Dots interligados
    public class Verlet
    {
        // Lista de nós da simulação (inicializada vazia)
        public List<Dot> Dots { get; } = new List<Dot>();

        // Massa global usada quando um nó não tem massa própria
        private float globalMass;

        // Quantidade de iterações para resolver ajustamentos de comprimento por frame
        private int iterations;

        // Acumulador de forças externas (ex.: gravidade) a aplicar
        private Vector3 CurrentForce = Vector3.zero;

        // Máscara de camadas para deteção de colisões 2D
        public LayerMask collisionLayerMask;

        // Raio de cada nó para cálculo de penetração em colisões
        public float dotRadius = 0.1f;

        // Inicializa simulador com massa global e número de iterações definidos
        public Verlet(float mass, int iterations)
        {
            this.globalMass = mass;
            this.iterations = iterations;
        }

        // Acumula força externa que será aplicada no próximo passo de simulação
        public void AddForce(Vector3 force)
        {
            CurrentForce += force;
        }

        // Executa um ciclo completo: física de pontos, restrições de comprimento e colisões
        public void simular(float deltaTime)
        {
            AplicarFisicaPontos(deltaTime);
            LimitarTamanho();
            AplicarColisoes();
        }

        // Integra posições dos nós pelo método de Verlet
        private void AplicarFisicaPontos(float deltaTime)
        {
            float dt2 = deltaTime * deltaTime;

            foreach (Dot dot in Dots)
            {
                if (dot.isLocked) continue;

                Vector3 acceleration = CurrentForce / (dot.mass > 0 ? dot.mass : globalMass);
                Vector3 variation = acceleration * dt2;

                Vector3 temp = dot.Currentposition;
                dot.Currentposition += (dot.Currentposition - dot.LastPosition) + variation;
                dot.LastPosition = temp;
            }

            CurrentForce = Vector3.zero;
        }

        // Ajusta distância entre cada par de nós conectados para manter comprimento fixo
        private void LimitarTamanho()
        {
            for (int i = 0; i < iterations; i++)
            {
                foreach (Dot dotA in Dots)
                {
                    foreach (Connection connection in dotA.connections)
                    {
                        Dot dotB = connection.Other(dotA);
                        Vector3 center = (dotA.Currentposition + dotB.Currentposition) * 0.5f;
                        Vector3 direction = (dotA.Currentposition - dotB.Currentposition).normalized;
                        Vector3 offset = direction * (connection.Length * 0.5f);

                        if (!dotA.isLocked)
                            dotA.Currentposition = center + offset;
                        if (!dotB.isLocked)
                            dotB.Currentposition = center - offset;
                    }
                }
            }
        }

        // Aplica força específica a um único nó utilizando o delta fixo de física
        public void AddForcePara(Dot dot, Vector3 force)
        {
            if (dot.isLocked) return;

            Vector3 acceleration = force / dot.mass;
            float dt2 = Time.fixedDeltaTime * Time.fixedDeltaTime;
            Vector3 temp = dot.Currentposition;

            dot.Currentposition += (dot.Currentposition - dot.LastPosition) + acceleration * dt2;
            dot.LastPosition = temp;
        }

        // Deteta e resolve colisões 2D, ajustando posição e amortecendo velocidade
        private void AplicarColisoes()
        {
            foreach (Dot dot in Dots)
            {
                if (dot.isLocked) continue;

                Collider2D hit = Physics2D.OverlapCircle(dot.Currentposition, dotRadius, collisionLayerMask);
                if (hit != null)
                {
                    Vector2 closest = hit.ClosestPoint(dot.Currentposition);
                    Vector2 dir = ((Vector2)dot.Currentposition - closest).normalized;

                    float dist = Vector2.Distance(dot.Currentposition, closest);
                    float pen = dotRadius - dist;
                    if (pen > 0f)
                    {
                        dot.Currentposition += (Vector3)(dir * (pen + 0.01f));
                        Vector3 vel = dot.Currentposition - dot.LastPosition;
                        dot.LastPosition = dot.Currentposition - vel * 0.5f;
                    }
                }
            }
        }
    }
}
