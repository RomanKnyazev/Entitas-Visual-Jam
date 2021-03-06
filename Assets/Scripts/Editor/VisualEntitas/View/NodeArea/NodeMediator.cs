﻿using System;
using Entitas.Visual.Model;
using Entitas.Visual.Model.VO;
using Entitas.Visual.Utils;
using Entitas.Visual.View.Drawer;
using PureMVC.Interfaces;
using PureMVC.Patterns.Mediator;
using UnityEditor;
using UnityEngine;

namespace Entitas.Visual.View
{
    public class NodeMediator : Mediator
    {
        public const string Name = "NodeMediator";
        public const float TitleMargins = 8f;

        public bool IsRenaming { get; private set; }
        public Node Node { get; private set; }

        private NodeTitleDrawer _nodeTitleDrawer;
        private NodeBackgroundDrawer _nodeBackgroundDrawer;
        private NodeFieldsAdditonWidgetDrawer _nodeFieldsAdditonWidgetDrawer;
        private NodeCollapseChevronDrawer _nodeCollapseChevronDrawer;
        private NodeFieldsDrawer _nodeFieldsDrawer;

        private GenericMenu _addFieldsGenericMenu;
        private GenericMenu _nodeContextGenericMenu;

        private Action<Field, Type> _onFieldTypeChanged;

        private FieldTypeProviderProxy _fieldTypeProviderProxy;

        public NodeMediator(string name, Node node, EditorWindow viewComponent) : base(name, viewComponent)
        {
            Node = node;
            _nodeBackgroundDrawer = new NodeBackgroundDrawer();
            _nodeTitleDrawer = new NodeTitleDrawer();
            _nodeFieldsAdditonWidgetDrawer = new NodeFieldsAdditonWidgetDrawer();
            _nodeCollapseChevronDrawer = new NodeCollapseChevronDrawer();
            _nodeFieldsDrawer = new NodeFieldsDrawer(node);

            _onFieldTypeChanged = OnChangeFieldType;
        }

        public override void OnRegister()
        {
            base.OnRegister();
            _fieldTypeProviderProxy = (FieldTypeProviderProxy)Facade.RetrieveProxy(FieldTypeProviderProxy.Name);
            _nodeFieldsDrawer.OnRegister(_fieldTypeProviderProxy, _onFieldTypeChanged);
        }

        public void OnGUI(EditorWindow appView)
        {
            if (_fieldTypeProviderProxy == null)
            {
                return;
            }

            PaintElements(appView);
        }

        public override string[] ListNotificationInterests()
        {
            return new[]
            {
                GraphProxy.NodeFieldRemoved,
                GraphProxy.NodeFieldAdded
            };
        }

        public override void HandleNotification(INotification notification)
        {
            switch (notification.Name)
            {
                case GraphProxy.NodeFieldRemoved:
                    var deletedField = (Tuple<Node, Field>) notification.Body;
                    if (deletedField.First == Node)
                    {
                        _nodeFieldsDrawer.HandleFieldRemoval(deletedField.Second);
                    }
                    break;
                case GraphProxy.NodeFieldAdded:
                    var addedField = (Tuple<Node, Field>)notification.Body;
                    if (addedField.First == Node)
                    {
                        _nodeFieldsDrawer.HandleFieldAddition(addedField.Second, _fieldTypeProviderProxy, _onFieldTypeChanged);
                    }
                    break;
            }
        }

        private void PaintElements(EditorWindow appView)
        {
            float titleHeight = 48f;
            float fieldsAdditionLineHeight = 32f;

            float fieldLineHeight = 22f;
            float chevronBackdropHeight = 16f;

            var isNodeCollapsed = Node.IsCollapsed;
            var nodeRect = Node.Position;
            nodeRect.height = isNodeCollapsed ? titleHeight : nodeRect.height;

            var titleRect = new Rect(nodeRect.x, nodeRect.y, nodeRect.width, titleHeight);
            var addFieldsRect = new Rect(nodeRect.x, titleRect.y + titleRect.height, nodeRect.width, fieldsAdditionLineHeight);
            addFieldsRect.height = isNodeCollapsed ? 0f : addFieldsRect.height;

            var fieldsRect = new Rect(nodeRect.x, addFieldsRect.y + addFieldsRect.height, nodeRect.width,
                fieldLineHeight * Node.Fields.Count);
            fieldsRect.height = isNodeCollapsed ? 0f : fieldsRect.height;

            var chevronRect = new Rect(nodeRect.x, fieldsRect.y + fieldsRect.height, nodeRect.width, chevronBackdropHeight);

            if (isNodeCollapsed)
            {
                chevronRect.y = nodeRect.y + titleRect.height;
            }

            var backgroundRect = new Rect(nodeRect.x, nodeRect.y, nodeRect.width,
                titleRect.height + addFieldsRect.height + fieldsRect.height);

            _nodeBackgroundDrawer.OnGUI(appView, backgroundRect, chevronRect.height);
            _nodeTitleDrawer.OnGUI(appView, titleRect, Node);

            float fieldsDesiredWidth = 0f;
            if (!isNodeCollapsed)
            {
                _nodeFieldsAdditonWidgetDrawer.OnGUI(appView, addFieldsRect);
                _nodeFieldsDrawer.OnGUI(fieldsRect, fieldLineHeight, Node);
                fieldsDesiredWidth = _nodeFieldsDrawer.GetDesiredWidth();
            }

            _nodeCollapseChevronDrawer.OnGUI(appView, chevronRect, isNodeCollapsed);

            IsRenaming = _nodeTitleDrawer.IsRenaming;

            float titleDesiredWidth = _nodeTitleDrawer.GetDesiredWidth();

            var desiredWidth = Mathf.Max(titleDesiredWidth, fieldsDesiredWidth);
            var newPositionRect = new Rect(Node.Position);
            newPositionRect.width = desiredWidth;

            if (newPositionRect.width < Node.DefaultWidth)
            {
                newPositionRect.width = Node.DefaultWidth;
            }

            int snapSteps = Mathf.CeilToInt(newPositionRect.width / GraphWindowMediator.CellWidth);
            newPositionRect.width = GraphWindowMediator.CellWidth * snapSteps;

            if (!Mathf.Approximately(newPositionRect.width, Node.Position.width))
            {
                Node.Position = newPositionRect;
                SendNotification(NodeAreaMediator.NodeResize, new Tuple<Node, Rect>(Node, newPositionRect));
            }
        }

        public void HandleEvents()
        {
            if (_fieldTypeProviderProxy == null)
            {
                return;
            }

            var currentEvent = Event.current;
            if (currentEvent.type == EventType.Repaint || currentEvent.type == EventType.Layout)
            {
                return;
            }

            var chevronPressed = _nodeCollapseChevronDrawer.HandleEvents(currentEvent);
            if (chevronPressed)
            {
                SendNotification(NodeAreaMediator.NodeCollapse, Node);
            }

            if (_addFieldsGenericMenu == null)
            {
                _addFieldsGenericMenu = new GenericMenu();
                foreach (var fieldType in _fieldTypeProviderProxy.FieldTypeProviderData.Types)
                {
                    var localField = fieldType;
                    _addFieldsGenericMenu.AddItem(new GUIContent(localField.Name), false, () => { OnAddFieldToNode(localField); });
                    _addFieldsGenericMenu.AddItem(new GUIContent(localField.Name), false, () => { OnAddFieldToNode(localField); });
                    _addFieldsGenericMenu.AddItem(new GUIContent(localField.Name), false, () => { OnAddFieldToNode(localField); });
                    _addFieldsGenericMenu.AddItem(new GUIContent(localField.Name), false, () => { OnAddFieldToNode(localField); });
                    _addFieldsGenericMenu.AddItem(new GUIContent(localField.Name), false, () => { OnAddFieldToNode(localField); });
                }
            }

            if (_nodeContextGenericMenu == null)
            {
                _nodeContextGenericMenu = new GenericMenu();
                _nodeContextGenericMenu.AddItem(new GUIContent("Remove"), false, OnRemoveNode);
            }

            _nodeBackgroundDrawer.HandleRightClick(currentEvent, _nodeContextGenericMenu);
            _nodeFieldsAdditonWidgetDrawer.HandleEvents(currentEvent, _addFieldsGenericMenu);

            Field renamedField;
            var removedField = _nodeFieldsDrawer.HandleEvents(currentEvent, Node, out renamedField);
            if (removedField != null)
            {
                SendNotification(NodeAreaMediator.NodeFieldRemove, new Tuple<Node, Field>(Node, removedField));
            }
            if (renamedField != null)
            {
                SendNotification(NodeAreaMediator.NodeFieldRename, new Tuple<Node, Field>(Node, renamedField));
            }

            var titleChanged = _nodeTitleDrawer.HandleOnGUI(currentEvent);
            if (titleChanged)
            {
                SendNotification(NodeAreaMediator.NodeRename, new Tuple<Node, string>(Node, Node.Name));
            }
            
            Vector2 dragNodePosition;
            Tuple<bool, bool> isNodeDraggedOrCompletedDragging = 
                _nodeBackgroundDrawer.HandleDrag(currentEvent, out dragNodePosition);

            if (isNodeDraggedOrCompletedDragging.First)
            {
                Node.Position.position = dragNodePosition;
            }
            if (isNodeDraggedOrCompletedDragging.Second)
            {
                SendNotification(NodeAreaMediator.NodePositionUpdate, new Tuple<Node, Vector2>(Node, dragNodePosition));
            }
        }

        private void OnAddFieldToNode(Type newType)
        {
            SendNotification(NodeAreaMediator.NodeFieldAdd, new Tuple<Node, Type>(Node, newType));
        }

        private void OnChangeFieldType(Field field, Type newType)
        {
            SendNotification(NodeAreaMediator.NodeFieldChangeType, new Tuple<Node, Field, Type>(Node, field, newType));
        }

        private void OnRemoveNode()
        {
            SendNotification(NodeAreaMediator.NodeRemove, Node);
        }
    }
}