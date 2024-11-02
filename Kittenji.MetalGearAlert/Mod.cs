using GDWeave;
using GDWeave.Godot.Variants;
using GDWeave.Godot;
using GDWeave.Modding;
using System.Text.Json.Serialization;
using System;

namespace Kittenji.MetalGearAlert;

public class Config {
    [JsonInclude] public float volume = 25;

    public static float LinearToDecibel(float linear) {
        float dB;
        if (linear != 0) dB = 20.0f * (float)Math.Log10(linear);
        else dB = -144.0f;
        return dB;
    }

    public int GetVolumeDB() {
        return (int)Math.Round(LinearToDecibel(Math.Clamp(volume, 0f, 100f) / 100f));
    }
}

public class Mod : IMod {
#pragma warning disable CS8618
    public static Config Config;
    public static Serilog.ILogger Logger;
#pragma warning restore CS8618

    public Mod(IModInterface modInterface) {
        Logger = modInterface.Logger;
        Config = modInterface.ReadConfig<Config>();

        Logger.Information("Converted volume: " + Config.GetVolumeDB());

        modInterface.RegisterScriptMod(new PlayerFishCatchPatch());
    }

    public void Dispose() {

    }
}

public class PlayerFishCatchPatch : IScriptMod {
    private const string Notif = "notif_audio";
    private const string NotifSound = "notif_sound_fx";

    public bool ShouldRun(string path) => path == "res://Scenes/Entities/Player/player.gdc";

    // returns a list of tokens for the new script, with the input being the original script's tokens
    public IEnumerable<Token> Modify(string path, IEnumerable<Token> tokens) {
        // wait for any newline token after any extends token
        var waiter = new MultiTokenWaiter([
            t => t is IdentifierToken {Name: "_on_fish_catch_timer_timeout"},
            t => t.Type is TokenType.ParenthesisOpen,
            t => t.Type is TokenType.ParenthesisClose,
            t => t.Type is TokenType.Colon,
            t => t.Type is TokenType.PrVar,
            t => t is IdentifierToken {Name: "fish_roll"},
            t => t.Type is TokenType.Newline,
        ], allowPartialMatch: true);

        // https://github.com/danielah05/WebfishingMods/blob/main/EventAlert/MeteorSpawnPatch.cs
        foreach (var token in tokens) {
            if (waiter.Check(token)) {
                // found our match, return the original newline
                yield return token;

                // var notif = AudioStreamPlayer.new()
                yield return new Token(TokenType.Newline, 1);
                yield return new Token(TokenType.PrVar);
                yield return new IdentifierToken(Notif);
                yield return new Token(TokenType.OpAssign);
                yield return new IdentifierToken("AudioStreamPlayer");
                yield return new Token(TokenType.Period);
                yield return new IdentifierToken("new");
                yield return new Token(TokenType.ParenthesisOpen);
                yield return new Token(TokenType.ParenthesisClose);
                yield return new Token(TokenType.Newline, 1);
                // var notifsound = load("res://kittenji.mods/metal_gear_alert/mg_alert.ogg")
                yield return new Token(TokenType.PrVar);
                yield return new IdentifierToken(NotifSound);
                yield return new Token(TokenType.OpAssign);
                yield return new Token(TokenType.BuiltInFunc, 76);
                yield return new Token(TokenType.ParenthesisOpen);
                yield return new ConstantToken(new StringVariant("res://kittenji.mods/metal_gear_alert/mg_alert.ogg"));
                yield return new Token(TokenType.ParenthesisClose);
                yield return new Token(TokenType.Newline, 1);
                // add_child(notif)
                yield return new IdentifierToken("add_child");
                yield return new Token(TokenType.ParenthesisOpen);
                yield return new IdentifierToken(Notif);
                yield return new Token(TokenType.ParenthesisClose);
                yield return new Token(TokenType.Newline, 1);
                // notif.set_stream(notifsound)
                yield return new IdentifierToken(Notif);
                yield return new Token(TokenType.Period);
                yield return new IdentifierToken("set_stream");
                yield return new Token(TokenType.ParenthesisOpen);
                yield return new IdentifierToken(NotifSound);
                yield return new Token(TokenType.ParenthesisClose);
                yield return new Token(TokenType.Newline, 1);
                // notif.volume_db = -4
                yield return new IdentifierToken(Notif);
                yield return new Token(TokenType.Period);
                yield return new IdentifierToken("volume_db");
                yield return new Token(TokenType.OpAssign);
                yield return new ConstantToken(new IntVariant(Mod.Config.GetVolumeDB()));
                yield return new Token(TokenType.Newline, 1);
                // notif.pitch_scale = 1
                yield return new IdentifierToken(Notif);
                yield return new Token(TokenType.Period);
                yield return new IdentifierToken("pitch_scale");
                yield return new Token(TokenType.OpAssign);
                yield return new ConstantToken(new IntVariant(1));
                yield return new Token(TokenType.Newline, 1);
                // notif.bus = "SFX"
                yield return new IdentifierToken(Notif);
                yield return new Token(TokenType.Period);
                yield return new IdentifierToken("bus");
                yield return new Token(TokenType.OpAssign);
                yield return new ConstantToken(new StringVariant("SFX"));
                yield return new Token(TokenType.Newline, 1);
                // notif.play()
                yield return new IdentifierToken(Notif);
                yield return new Token(TokenType.Period);
                yield return new IdentifierToken("play");
                yield return new Token(TokenType.ParenthesisOpen);
                yield return new Token(TokenType.ParenthesisClose);
                yield return new Token(TokenType.Newline, 1);
            } else {
                // return the original token
                yield return token;
            }
        }
    }
}
