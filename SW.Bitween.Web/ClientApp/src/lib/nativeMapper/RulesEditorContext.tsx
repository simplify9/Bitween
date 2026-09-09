import { createContext, useContext, useReducer, type Dispatch, type ReactNode } from "react";
import {
  initialRulesEditorState,
  rulesEditorReducer,
  type RulesEditorAction,
  type RulesEditorState,
} from "./rulesReducer";

const StateContext = createContext<RulesEditorState | null>(null);
const DispatchContext = createContext<Dispatch<RulesEditorAction> | null>(null);

export function RulesEditorProvider({ children }: { children: ReactNode }) {
  const [state, dispatch] = useReducer(rulesEditorReducer, initialRulesEditorState);
  return (
    <StateContext.Provider value={state}>
      <DispatchContext.Provider value={dispatch}>{children}</DispatchContext.Provider>
    </StateContext.Provider>
  );
}

export function useRules(): RulesEditorState {
  const state = useContext(StateContext);
  if (!state) throw new Error("useRules must be used inside RulesEditorProvider");
  return state;
}

export function useRulesDispatch(): Dispatch<RulesEditorAction> {
  const dispatch = useContext(DispatchContext);
  if (!dispatch) throw new Error("useRulesDispatch must be used inside RulesEditorProvider");
  return dispatch;
}
