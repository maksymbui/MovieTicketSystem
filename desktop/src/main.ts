import { app, BrowserWindow } from 'electron';
import { spawn, ChildProcess } from 'node:child_process';
import path from 'node:path';
import url from 'node:url';

let backendProcess: ChildProcess | null = null;

const isDev = process.env.NODE_ENV === 'development';
const FRONTEND_DEV_SERVER = process.env.FRONTEND_URL ?? 'http://localhost:5173';

function resolveBackendDll() {
  const base = isDev ? 'Debug' : 'Release';
  return path.join(__dirname, '..', '..', 'backend', 'src', 'MovieTickets.Api', 'bin', base, 'net8.0', 'MovieTickets.Api.dll');
}

function startBackend() {
  const dllPath = resolveBackendDll();
  const workingDirectory = path.dirname(dllPath);
  const processHandle = spawn('dotnet', [dllPath], {
    stdio: 'inherit',
    cwd: workingDirectory
  });
  processHandle.on('exit', (code) => {
    console.log(`Backend exited with code ${code}`);
  });
  backendProcess = processHandle;
}

async function createWindow() {
  const win = new BrowserWindow({
    width: 1200,
    height: 800,
    backgroundColor: '#19181B',
    webPreferences: {
      contextIsolation: true
    }
  });

  if (isDev) {
    await win.loadURL(FRONTEND_DEV_SERVER);
  } else {
    const indexPath = path.join(__dirname, '..', '..', 'frontend', 'dist', 'index.html');
    await win.loadURL(url.pathToFileURL(indexPath).toString());
  }
}

app.whenReady().then(() => {
  if (!isDev) {
    startBackend();
  }
  createWindow();

  app.on('activate', () => {
    if (BrowserWindow.getAllWindows().length === 0) {
      createWindow();
    }
  });
});

app.on('window-all-closed', () => {
  if (process.platform !== 'darwin') {
    app.quit();
  }
});

app.on('quit', () => {
  if (backendProcess) {
    backendProcess.kill();
  }
});
