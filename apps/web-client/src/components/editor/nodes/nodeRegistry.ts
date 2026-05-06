import type { Component } from 'vue'
import {
  BUTTON_BRANCHING_NODE_TYPE,
  DATA_NODE_TYPE,
  EDIT_MESSAGE_NODE_TYPE,
  GROUP_ENTRY_NODE_TYPE,
  GROUP_EXIT_NODE_TYPE,
  GOTO_NODE_TYPE,
  MESSAGE_NODE_TYPE,
} from '@/components/editor/editorTypes'
import DataNode from './DataNode.vue'
import DeleteMessageNode from './DeleteMessageNode.vue'
import DelayNode from './DelayNode.vue'
import EditMessageNode from './EditMessageNode.vue'
import GenericNode from './GenericNode.vue'
import GroupBoundaryNode from './GroupBoundaryNode.vue'
import GotoNode from './GotoNode.vue'
import HttpRequestNode from './HttpRequestNode.vue'
import MessageNode from './MessageNode.vue'
import ReceiveImageNode from './ReceiveImageNode.vue'
import ReceiveMessageNode from './ReceiveMessageNode.vue'
import SendImageNode from './SendImageNode.vue'
import ConditionNode from './ConditionNode.vue'
import SetVariableNode from './SetVariableNode.vue'
import SwitchNode from './SwitchNode.vue'
import TypingIndicatorNode from './TypingIndicatorNode.vue'
import UserInfoNode from './UserInfoNode.vue'

/**
 * Реестр самостоятельных UI-узлов.
 * GenericNode — только компактный fallback-preview, не рендер формы из каталога.
 */
const NODE_COMPONENTS: Readonly<Record<string, Component>> = {
  [BUTTON_BRANCHING_NODE_TYPE]: MessageNode,
  [DATA_NODE_TYPE]: DataNode,
  [MESSAGE_NODE_TYPE]: MessageNode,
  [GROUP_ENTRY_NODE_TYPE]: GroupBoundaryNode,
  [GROUP_EXIT_NODE_TYPE]: GroupBoundaryNode,
  condition: ConditionNode,
  delete_message: DeleteMessageNode,
  delay: DelayNode,
  [GOTO_NODE_TYPE]: GotoNode,
  [EDIT_MESSAGE_NODE_TYPE]: EditMessageNode,
  receive_image: ReceiveImageNode,
  receive_message: ReceiveMessageNode,
  send_image: SendImageNode,
  set_variable: SetVariableNode,
  switch: SwitchNode,
  typing_indicator: TypingIndicatorNode,
  get_user_info: UserInfoNode,
  http_request: HttpRequestNode,
}

export function getNodeComponent(type: string): Component {
  return NODE_COMPONENTS[type] ?? GenericNode
}
