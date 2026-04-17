import { Editor, rootDOMCtx, defaultValueCtx } from '@milkdown/kit/core';
import { commonmark } from '@milkdown/kit/preset/commonmark';
import { nord } from '@milkdown/theme-nord';
import { history } from '@milkdown/kit/plugin/history';
import { listener, listenerCtx } from '@milkdown/kit/plugin/listener';
import { replaceAll } from '@milkdown/kit/utils';

// Import CSS
import '@milkdown/theme-nord/style.css';
import './style.css';

async function createEditor() {
  const appRoot = document.querySelector('#app');
  const win = window as any;

  if (!appRoot) {
    if (win.chrome?.webview) win.chrome.webview.postMessage({ type: 'LOG', message: 'ERROR: #app not found' });
    return null;
  }

  if (win.chrome?.webview) win.chrome.webview.postMessage({ type: 'LOG', message: 'Creating editor with commonmark...' });

  try {
    const editor = await Editor.make()
      .config((ctx) => {
        ctx.set(rootDOMCtx, appRoot as HTMLElement);
        ctx.set(defaultValueCtx, '# Sharpora\n\nWelcome to your new Markdown editor. Start writing!');

        const inst = ctx.get(listenerCtx);
        inst.markdownUpdated((_ctx, markdown, prevMarkdown) => {
          if (markdown !== prevMarkdown) {
            const words = markdown.trim().split(/\s+/).filter(w => w.length > 0).length;
            if (win.chrome?.webview) {
              win.chrome.webview.postMessage({
                type: 'CONTENT_CHANGED',
                content: markdown,
                wordCount: words
              });
            }
          }
        });
      })
      .config(nord)
      .use(commonmark) // Use commonmark for stability test
      .use(history)
      .use(listener)
      .create();

    if (win.chrome?.webview) win.chrome.webview.postMessage({ type: 'LOG', message: 'Editor created successfully' });

    // Auto focus
    setTimeout(() => {
      const editable = document.querySelector('.editor');
      if (editable) {
        (editable as HTMLElement).focus();
        if (win.chrome?.webview) win.chrome.webview.postMessage({ type: 'LOG', message: 'Focused editor' });
      }
    }, 500);

    return editor;
  } catch (e: any) {
    if (win.chrome?.webview) win.chrome.webview.postMessage({ type: 'LOG', message: 'ERROR: ' + e.message });
    console.error(e);
    throw e;
  }
}

const editorPromise = createEditor();

const win = window as any;
if (win.chrome && win.chrome.webview) {
  win.chrome.webview.addEventListener('message', async (event: any) => {
    const message = event.data;
    const editor = await editorPromise;
    if (!editor) return;

    if (message.type === 'SET_CONTENT') {
      editor.action(replaceAll(message.content));
    }
  });
}
