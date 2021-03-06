﻿using Entitas.Visual.Utils;
using UnityEditor;
using UnityEngine;

namespace Entitas.Visual.View.Drawer
{
    public class GraphWindow
    {
        public void OnGUI(EditorWindow window)
        {
            DrawBackground(window.position);
            DrawGrid(window.position);
        }

        private void DrawBackground(Rect position)
        {
            GUIHelper.DrawQuad(new Rect(0f, 0f, position.width, position.height), StyleProxy.DarkBackgroundColor);
        }

        private void DrawGrid(Rect position)
        {
            GL.PushMatrix();
            GL.Begin(GL.LINES);
            DrawGridLines(16f, StyleProxy.DarkLineColorMinor, position);
            DrawGridLines(80f, StyleProxy.DarkLineColorMajor, position);
            GL.End();
            GL.PopMatrix();
        }

        private void DrawGridLines(float gridSize, Color gridColor, Rect position)
        {
            if (Event.current.type != EventType.Repaint)
            {
                return;
            }

            GL.Color(gridColor);
            float x = 0f;
            while (x < position.width)
            {
                this.DrawLine(
                    new Vector2(x, 16f),
                    new Vector2(x, position.height));

                x += gridSize;
            }

            float y = 32f;
            while (y < position.height)
            {
                this.DrawLine(
                    new Vector2(0f, y),
                    new Vector2(position.width, y));

                y += gridSize;
            }
        }

        private void DrawLine(Vector2 p1, Vector2 p2)
        {
            GL.Vertex(p1);
            GL.Vertex(p2);
        }
    }
}
