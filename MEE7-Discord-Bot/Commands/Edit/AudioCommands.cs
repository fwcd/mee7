﻿using Discord.Audio;
using Discord.WebSocket;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MEE7.Backend;
using MEE7.Backend.HelperFunctions;
using NAudio.Wave.SampleProviders;

namespace MEE7.Commands
{
    public partial class Edit : Command
    {
        readonly EditCommand[] AudioCommands = new EditCommand[] {
            new EditCommand("playAudio", "Plays audio in voicechat", typeof(WaveStream), null, new Argument[0], (SocketMessage m, object[] args, object o) => {
                SocketGuild g = Program.GetGuildFromChannel(m.Channel);
                ISocketAudioChannel channel = g.VoiceChannels.FirstOrDefault(x => x.Users.Select(y => y.Id).Contains(m.Author.Id));
                if (channel != null)
                {
                    try { channel.DisconnectAsync().Wait(); } catch { }

                    IAudioClient client = channel.ConnectAsync().Result;
                    using (WaveStream naudioStream = WaveFormatConversionStream.CreatePcmStream(o as WaveStream))
                            MultiMediaHelper.SendAudioAsync(client, naudioStream).Wait();

                    try { channel.DisconnectAsync().Wait(); } catch { }
                }
                else
                    DiscordNETWrapper.SendText("You are not in an AudioChannel on this server!", m.Channel).Wait();

                (o as WaveStream).Dispose();
                return null;
            }),
            new EditCommand("drawAudio", "Draw the samples", typeof(WaveStream), typeof(Bitmap), new Argument[0], (SocketMessage m, object[] args, object o) => {

                WaveStream w = o as WaveStream;
                var c = new WaveChannel32(w);

                float[] normSamplesMin = new float[1000]; Array.Fill(normSamplesMin, 250);
                float[] normSamplesMax = new float[1000]; Array.Fill(normSamplesMax, 250);
                byte[] buffer = new byte[1024];
                int reader = 0;
                while (c.Position < c.Length)
                {
                    reader = c.Read(buffer, 0, buffer.Length);
                    for (int i = 0; i < buffer.Length / 4; i++)
                    {
                        float sample = BitConverter.ToSingle(buffer, i * 4) * 200 + 250;
                        int index = (int)(((c.Position - buffer.Length) / 4.0 + i) / (c.Length / 4.0) * 1000);
                        if (index >= normSamplesMax.Length)
                            continue;
                        if (normSamplesMax[index] < sample)
                            normSamplesMax[index] = sample;
                        if (normSamplesMin[index] > sample)
                            normSamplesMin[index] = sample;
                    }
                }

                int j = 0;
                Bitmap output = new Bitmap(1000, 500);
                using (Graphics graphics = Graphics.FromImage(output))
                    graphics.DrawLines(new Pen(Color.White), Enumerable.
                        Range(0, 1000).
                        Select(x => new Point[] {
                            new Point(j, (int)normSamplesMin[x]),
                            new Point(j++, (int)normSamplesMax[x]),
                        }).
                        SelectMany(x => x).
                        ToArray());

                w.Dispose();
                return output;

            }),
            new EditCommand("pitch", "Adds a Pitch to the sound", typeof(WaveStream), typeof(WaveStream), new Argument[] { new Argument("PitchFactor", typeof(float), null) },
                    (SocketMessage m, object[] args, object o) => {

                        string filePath = $"Commands{Path.DirectorySeparatorChar}Edit{Path.DirectorySeparatorChar}pitch.bin";

                        SmbPitchShiftingSampleProvider pitch = new SmbPitchShiftingSampleProvider((o as WaveStream).ToSampleProvider())
                        {
                            PitchFactor = (float)args[0]
                        };

                        WaveFileWriter.CreateWaveFile16(filePath, pitch);
                        return new WaveFileReader(filePath);
            }),
            new EditCommand("volume", "Adds Volume to the sound", typeof(WaveStream), typeof(WaveStream), new Argument[] { new Argument("VolumeFactor", typeof(float), null) },
                    (SocketMessage m, object[] args, object o) => {

                        string filePath = $"Commands{Path.DirectorySeparatorChar}Edit{Path.DirectorySeparatorChar}volume.bin";

                        VolumeSampleProvider  pitch = new VolumeSampleProvider((o as WaveStream).ToSampleProvider())
                        {
                            Volume = (float)args[0]
                        };

                        WaveFileWriter.CreateWaveFile16(filePath, pitch);
                        return new WaveFileReader(filePath);
            }),
        };
    }
}
