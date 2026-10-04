using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Media;
using System.Speech.Synthesis;
using System.Text;
using System.Windows.Forms;

class Voz
{
    public string Nombre, Motor, Id;
    public bool Preset;
    public int PVel, PTono;
    public override string ToString() { return Nombre; }
}

class LoquendoTTS : Form
{
    SpeechSynthesizer synth = new SpeechSynthesizer();
    SoundPlayer player;
    TextBox txt = new TextBox();
    ComboBox voices = new ComboBox();
    TrackBar rate, pitch, vol;
    Label info = new Label();
    string baseDir = AppDomain.CurrentDomain.BaseDirectory;

    [STAThread]
    static void Main()
    {
        Application.EnableVisualStyles();
        Application.Run(new LoquendoTTS());
    }

    Label L(string t, int x, int y)
    {
        var l = new Label(); l.Text = t; l.Left = x; l.Top = y; l.AutoSize = true; return l;
    }

    Button B(string t, int x, int y, EventHandler h)
    {
        var b = new Button(); b.Text = t; b.Left = x; b.Top = y; b.Width = 110; b.Height = 32; b.Click += h; return b;
    }

    TrackBar T(int min, int max, int val, int x, int y)
    {
        var t = new TrackBar(); t.Minimum = min; t.Maximum = max; t.Value = val;
        t.Left = x; t.Top = y; t.Width = 160; t.TickFrequency = 2; return t;
    }

    public LoquendoTTS()
    {
        Text = "Voz Loquendo - Texto a voz";
        ClientSize = new Size(560, 460);
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;

        Controls.Add(L("Texto:", 12, 10));
        txt.Left = 12; txt.Top = 30; txt.Width = 536; txt.Height = 180;
        txt.Multiline = true; txt.ScrollBars = ScrollBars.Vertical;
        txt.MaxLength = int.MaxValue; // sin limite de caracteres
        txt.Font = new Font("Segoe UI", 11);
        txt.Text = "Hola, esta es una voz estilo Loquendo.";
        Controls.Add(txt);

        Controls.Add(L("Voz:", 12, 222));
        voices.Left = 12; voices.Top = 242; voices.Width = 536; voices.DropDownStyle = ComboBoxStyle.DropDownList;
        CargarVoces();
        voices.SelectedIndexChanged += delegate { ActualizarInfo(); };
        Controls.Add(voices);

        info.Left = 12; info.Top = 270; info.AutoSize = true; info.ForeColor = Color.DimGray;
        Controls.Add(info);

        Controls.Add(L("Velocidad", 12, 296)); rate = T(-10, 10, 0, 12, 314); Controls.Add(rate);
        Controls.Add(L("Tono", 192, 296)); pitch = T(-10, 10, 0, 192, 314); Controls.Add(pitch);
        Controls.Add(L("Volumen", 372, 296)); vol = T(0, 100, 100, 372, 314); vol.TickFrequency = 10; Controls.Add(vol);

        Controls.Add(B("Hablar", 12, 410, delegate { Hablar(); }));
        Controls.Add(B("Detener", 132, 410, delegate { Detener(); }));
        Controls.Add(B("Guardar WAV", 252, 410, delegate { GuardarWav(); }));
        Controls.Add(B("Salir", 438, 410, delegate { Close(); }));
        ActualizarInfo();
    }

    void CargarVoces()
    {
        // Piper: voces neuronales (las mas naturales)
        string vd = Path.Combine(baseDir, "voces");
        if (File.Exists(Path.Combine(baseDir, "piper", "piper.exe")) && Directory.Exists(vd))
            foreach (var f in Directory.GetFiles(vd, "*.onnx"))
                voices.Items.Add(new Voz { Nombre = "[Piper] " + Path.GetFileNameWithoutExtension(f), Motor = "piper", Id = f });

        // eSpeak NG: voz robotica clasica
        if (File.Exists(Path.Combine(baseDir, "espeak", "espeak-ng.exe")))
        {
            string[,] e = {
                {"Espanol Espana", "es"}, {"Espanol Latino", "es-419"},
                {"Espanol Espana (grave)", "es+m3"}, {"Espanol Latino (grave)", "es-419+m3"},
                {"Espanol Espana (mujer)", "es+f3"}, {"Espanol Latino (mujer)", "es-419+f3"},
                {"Espanol Latino (robot)", "es-419+croak"}, {"Espanol Espana (susurro)", "es+whisper"}
            };
            for (int i = 0; i < e.GetLength(0); i++)
                voices.Items.Add(new Voz { Nombre = "[eSpeak] " + e[i, 0], Motor = "espeak", Id = e[i, 1] });
        }

        // Voces instaladas en Windows
        foreach (var v in synth.GetInstalledVoices())
            if (v.Enabled)
                voices.Items.Add(new Voz { Nombre = "[Windows] " + v.VoiceInfo.Name + " (" + v.VoiceInfo.Culture.Name + ")", Motor = "sapi", Id = v.VoiceInfo.Name });

        // Presets "estilo Loquendo": voz base + velocidad/tono ajustados (aproximacion, no son las voces originales)
        string[,] pre = {
            // nombre, voz base (texto contenido), velocidad, tono
            {"Jorge (hombre, Espana)", "davefx", "-1", "0"},
            {"Diego (hombre, Latino)", "ald", "0", "0"},
            {"Carlos (hombre, Latino grave)", "claude", "-1", "0"},
            {"Soledad (mujer, Espana)", "Espana (mujer)", "0", "2"},
            {"Francisca (mujer, Latino)", "Latino (mujer)", "0", "2"},
            {"Esperanza (mujer, Mexico)", "Sabina", "0", "0"}
        };
        int pos = 0;
        for (int i = 0; i < pre.GetLength(0); i++)
            foreach (Voz b in voices.Items)
                if (b.Preset == false && b.Nombre.Contains(pre[i, 1]))
                {
                    voices.Items.Insert(pos++, new Voz { Nombre = "[Estilo Loquendo] " + pre[i, 0], Motor = b.Motor, Id = b.Id,
                        Preset = true, PVel = int.Parse(pre[i, 2]), PTono = int.Parse(pre[i, 3]) });
                    break;
                }

        if (voices.Items.Count > 0) voices.SelectedIndex = 0;
    }

    Voz Actual() { return voices.SelectedItem as Voz; }

    void ActualizarInfo()
    {
        var v = Actual();
        if (v == null) { info.Text = ""; return; }
        if (v.Preset) { rate.Value = v.PVel; pitch.Value = v.PTono; }
        info.Text = v.Motor == "piper" ? "Piper: el tono no se puede cambiar en esta voz." : "";
        pitch.Enabled = v.Motor != "piper";
    }

    // Genera un WAV con el motor elegido
    Voz cVoz; int cRate, cPitch, cVol;

    void Snap()
    {
        cVoz = Actual(); cRate = rate.Value; cPitch = pitch.Value; cVol = vol.Value;
        if (cVoz == null) throw new Exception("No hay voces disponibles.");
    }

    // Divide el texto en trozos (frases) de hasta ~400 caracteres
    static List<string> Trozos(string t)
    {
        var res = new List<string>();
        var sb = new StringBuilder();
        foreach (string parte in System.Text.RegularExpressions.Regex.Split(t, @"(?<=[\.\!\?\n;:])"))
        {
            if (sb.Length + parte.Length > 400 && sb.Length > 0) { res.Add(sb.ToString()); sb.Length = 0; }
            string p = parte;
            while (p.Length > 400)
            {
                int cut = p.LastIndexOf(' ', 400); if (cut < 100) cut = 400;
                res.Add(p.Substring(0, cut)); p = p.Substring(cut);
            }
            sb.Append(p);
        }
        if (sb.Length > 0) res.Add(sb.ToString());
        res.RemoveAll(delegate (string s) { return s.Trim().Length == 0; });
        return res;
    }

    string Sintetizar(string destino, string texto)
    {
        var v = cVoz;
        int rateV = cRate, pitchV = cPitch, volV = cVol;
        if (v.Motor == "sapi")
        {
            using (var s = new SpeechSynthesizer())
            {
                s.SelectVoice(v.Id);
                s.Rate = rateV;
                s.Volume = volV;
                s.SetOutputToWaveFile(destino);
                string pct = (pitchV * 10).ToString("+0;-0;0", CultureInfo.InvariantCulture) + "%";
                string lang = s.Voice.Culture.Name;
                string ssml = "<speak version=\"1.0\" xmlns=\"http://www.w3.org/2001/10/synthesis\" xml:lang=\"" + lang + "\">"
                    + "<prosody pitch=\"" + pct + "\">" + System.Security.SecurityElement.Escape(texto) + "</prosody></speak>";
                s.Speak(new Prompt(ssml, SynthesisTextFormat.Ssml));
            }
            return destino;
        }

        ProcessStartInfo psi = new ProcessStartInfo();
        psi.UseShellExecute = false; psi.CreateNoWindow = true;
        psi.RedirectStandardInput = true; psi.RedirectStandardError = true; psi.RedirectStandardOutput = true;
        psi.StandardOutputEncoding = Encoding.UTF8;
        if (v.Motor == "piper")
        {
            double ls = Math.Max(0.4, 1.0 - rateV * 0.06);
            psi.FileName = Path.Combine(baseDir, "piper", "piper.exe");
            psi.WorkingDirectory = Path.Combine(baseDir, "piper");
            psi.Arguments = "-m \"" + v.Id + "\" -f \"" + destino + "\" --length_scale " + ls.ToString("0.00", CultureInfo.InvariantCulture);
            using (var p = Process.Start(psi))
            {
                var w = new StreamWriter(p.StandardInput.BaseStream, new UTF8Encoding(false));
                w.Write(texto.Replace("\r\n", " ").Replace("\n", " ")); w.Close();
                p.StandardError.ReadToEnd(); p.WaitForExit();
            }
            EscalarVolumen(destino, volV / 100.0);
        }
        else
        {
            string dir = Path.Combine(baseDir, "espeak");
            psi.FileName = Path.Combine(dir, "espeak-ng.exe");
            int velocidad = 150 + rateV * 15;
            int tono = Math.Max(0, Math.Min(99, 50 + pitchV * 5));
            psi.Arguments = "--path=\"" + dir + "\" -v " + v.Id + " -s " + velocidad + " -p " + tono + " -a 150 -w \"" + destino + "\" --stdin";
            using (var p = Process.Start(psi))
            {
                var w = new StreamWriter(p.StandardInput.BaseStream, new UTF8Encoding(false));
                w.Write(texto); w.Close();
                p.StandardError.ReadToEnd(); p.WaitForExit();
            }
            EscalarVolumen(destino, volV / 100.0);
        }
        return destino;
    }

    // WAV PCM 16 bits: multiplica las muestras por el volumen
    static void EscalarVolumen(string wav, double k)
    {
        if (k >= 0.999 || !File.Exists(wav)) return;
        byte[] b = File.ReadAllBytes(wav);
        int pos = 12;
        while (pos + 8 <= b.Length)
        {
            string id = Encoding.ASCII.GetString(b, pos, 4);
            int len = BitConverter.ToInt32(b, pos + 4);
            if (id == "data")
            {
                int end = Math.Min(b.Length, pos + 8 + (len < 0 ? b.Length : len));
                for (int i = pos + 8; i + 1 < end; i += 2)
                {
                    short s = BitConverter.ToInt16(b, i);
                    short n = (short)(s * k);
                    b[i] = (byte)(n & 0xFF); b[i + 1] = (byte)((n >> 8) & 0xFF);
                }
                break;
            }
            pos += 8 + len + (len & 1);
        }
        File.WriteAllBytes(wav, b);
    }

    volatile int gen = 0;

    static string Tmp() { return Path.Combine(Path.GetTempPath(), "loquendo_" + Guid.NewGuid().ToString("N") + ".wav"); }

    // Habla por trozos: genera el siguiente mientras suena el anterior (sin limite de texto)
    void Hablar()
    {
        try
        {
            Detener();
            Snap();
            var trozos = Trozos(txt.Text);
            int miGen = ++gen;
            var cola = new System.Collections.Concurrent.BlockingCollection<string>(3);

            var prod = new System.Threading.Thread(delegate ()
            {
                try
                {
                    foreach (string t in trozos)
                    {
                        if (miGen != gen) break;
                        string f = Tmp();
                        Sintetizar(f, t);
                        cola.Add(f);
                    }
                }
                catch (Exception ex)
                {
                    if (miGen == gen) BeginInvoke(new MethodInvoker(delegate { MessageBox.Show(ex.Message, "Error"); }));
                }
                finally { cola.CompleteAdding(); }
            });
            prod.IsBackground = true; prod.Start();

            var cons = new System.Threading.Thread(delegate ()
            {
                foreach (string f in cola.GetConsumingEnumerable())
                {
                    try
                    {
                        if (miGen == gen && File.Exists(f))
                        {
                            var sp = new SoundPlayer(f);
                            player = sp;
                            sp.PlaySync();
                            sp.Dispose();
                        }
                    }
                    catch { }
                    try { File.Delete(f); } catch { }
                }
            });
            cons.IsBackground = true; cons.Start();
        }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Error"); }
    }

    void Detener()
    {
        gen++;
        var p = player;
        if (p != null) { try { p.Stop(); } catch { } }
    }

    void GuardarWav()
    {
        var d = new SaveFileDialog(); d.Filter = "WAV|*.wav"; d.FileName = "voz.wav";
        if (d.ShowDialog() != DialogResult.OK) return;
        try
        {
            Snap();
            Cursor = Cursors.WaitCursor;
            var archivos = new List<string>();
            foreach (string t in Trozos(txt.Text))
            {
                string f = Tmp();
                Sintetizar(f, t);
                archivos.Add(f);
                Application.DoEvents();
            }
            UnirWav(archivos, d.FileName);
            foreach (string f in archivos) { try { File.Delete(f); } catch { } }
            Cursor = Cursors.Default;
            MessageBox.Show("Guardado: " + d.FileName);
        }
        catch (Exception ex) { Cursor = Cursors.Default; MessageBox.Show(ex.Message, "Error"); }
    }

    static int BuscarData(byte[] b)
    {
        int pos = 12;
        while (pos + 8 <= b.Length)
        {
            int len = BitConverter.ToInt32(b, pos + 4);
            if (Encoding.ASCII.GetString(b, pos, 4) == "data") return pos;
            pos += 8 + len + (len & 1);
        }
        return -1;
    }

    // Une WAVs con el mismo formato en un solo archivo
    static void UnirWav(List<string> archivos, string salida)
    {
        if (archivos.Count == 0) throw new Exception("No hay texto.");
        using (var o = new FileStream(salida, FileMode.Create))
        {
            byte[] primero = File.ReadAllBytes(archivos[0]);
            int dp = BuscarData(primero);
            o.Write(primero, 0, dp + 8);
            long total = 0;
            foreach (string f in archivos)
            {
                byte[] b = File.ReadAllBytes(f);
                int p = BuscarData(b);
                int decl = BitConverter.ToInt32(b, p + 4);
                int len = Math.Min(decl < 0 ? int.MaxValue : decl, b.Length - p - 8);
                o.Write(b, p + 8, len); total += len;
            }
            o.Seek(4, SeekOrigin.Begin); o.Write(BitConverter.GetBytes((int)(total + dp)), 0, 4);
            o.Seek(dp + 4, SeekOrigin.Begin); o.Write(BitConverter.GetBytes((int)total), 0, 4);
        }
    }
}
