import { EditorView, basicSetup } from 'codemirror';
import { markdown, markdownLanguage } from '@codemirror/lang-markdown';
import { languages } from '@codemirror/language-data';
import { oneDark } from '@codemirror/theme-one-dark';
import { EditorState } from '@codemirror/state';
import { keymap } from '@codemirror/view';

const win = window;
let view = null;
let isDark = window.matchMedia('(prefers-color-scheme: dark)').matches;

function postMessage(msg) {
  if (win.chrome?.webview) {
    win.chrome.webview.postMessage(msg);
  }
}

// Light theme
const lightTheme = EditorView.theme({
  '&': { height: '100%', fontSize: '14px' },
  '.cm-scroller': { overflow: 'auto', fontFamily: "'Cascadia Code', 'Consolas', monospace" },
  '.cm-content': { padding: '16px 24px', maxWidth: '100%' },
  '.cm-gutters': { background: 'rgba(0,0,0,0.02)', border: 'none' },
  '.cm-activeLineGutter': { background: 'rgba(0,0,0,0.05)' },
  '.cm-activeLine': { background: 'rgba(0,0,0,0.03)' },
});

// Dark theme overrides
const darkTheme = EditorView.theme({
  '&': { height: '100%', fontSize: '14px' },
  '.cm-scroller': { overflow: 'auto', fontFamily: "'Cascadia Code', 'Consolas', monospace" },
  '.cm-content': { padding: '16px 24px', maxWidth: '100%' },
});

function createEditor(content, dark) {
  if (view) {
    view.destroy();
  }

  const extensions = [
    basicSetup,
    markdown({ base: markdownLanguage, codeLanguages: languages }),
    EditorView.lineWrapping,
    EditorView.updateListener.of((update) => {
      if (update.docChanged) {
        const text = update.state.doc.toString();
        const words = text.trim().split(/\s+/).filter(w => w.length > 0).length;
        postMessage({ type: 'CONTENT_CHANGED', content: text, wordCount: words });
      }
    }),
    keymap.of([
      {
        key: 'Mod-s',
        run: () => { postMessage({ type: 'SAVE_REQUEST' }); return true; },
      },
      {
        key: 'Escape',
        run: () => { postMessage({ type: 'EXIT_EDIT_MODE' }); return true; },
      },
    ]),
  ];

  if (dark) {
    extensions.push(oneDark);
    extensions.push(darkTheme);
  } else {
    extensions.push(lightTheme);
  }

  view = new EditorView({
    state: EditorState.create({
      doc: content || '',
      extensions,
    }),
    parent: document.querySelector('#editor-root'),
  });

  // Expose for C# ExecuteScriptAsync
  win.__komdGetContent = () => view.state.doc.toString();

  postMessage({ type: 'EDITOR_READY' });
}

// Listen for messages from C#
if (win.chrome?.webview) {
  win.chrome.webview.addEventListener('message', (event) => {
    const msg = event.data;

    if (msg.type === 'SET_CONTENT') {
      if (view) {
        view.dispatch({
          changes: { from: 0, to: view.state.doc.length, insert: msg.content || '' },
        });
      } else {
        createEditor(msg.content || '', msg.dark || false);
      }
    } else if (msg.type === 'INIT') {
      createEditor(msg.content || '', msg.dark || false);
    }
  });
}

// Initialize empty
createEditor('', isDark);
