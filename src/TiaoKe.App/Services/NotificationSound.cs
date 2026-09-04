using System.Media;
using System.IO;
using System.Windows;

namespace TiaoKe.App.Services;

public sealed class NotificationSound : IDisposable
{
    private readonly Stream? _reminderStream;
    private readonly Stream? _restCompleteStream;
    private readonly SoundPlayer? _reminderPlayer;
    private readonly SoundPlayer? _restCompletePlayer;

    public NotificationSound()
    {
        (_reminderStream, _reminderPlayer) = Load("tiaoke-reminder.wav");
        (_restCompleteStream, _restCompletePlayer) = Load("tiaoke-rest-complete.wav");
    }

    public void PlayReminder()
    {
        Play(_reminderPlayer);
    }

    public void PlayRestComplete()
    {
        Play(_restCompletePlayer);
    }

    public void Dispose()
    {
        _reminderPlayer?.Stop();
        _restCompletePlayer?.Stop();
        _reminderPlayer?.Dispose();
        _restCompletePlayer?.Dispose();
        _reminderStream?.Dispose();
        _restCompleteStream?.Dispose();
    }

    private static (Stream? Stream, SoundPlayer? Player) Load(string fileName)
    {
        try
        {
            var resource = System.Windows.Application.GetResourceStream(
                new Uri($"pack://application:,,,/Assets/{fileName}"));
            if (resource is null) return (null, null);

            var stream = resource.Stream;
            var player = new SoundPlayer(stream);
            player.Load();
            return (stream, player);
        }
        catch (InvalidOperationException)
        {
            return (null, null);
        }
        catch (IOException)
        {
            return (null, null);
        }
    }

    private static void Play(SoundPlayer? player)
    {
        if (player is null) return;
        try
        {
            player.Stop();
            player.Play();
        }
        catch (InvalidOperationException)
        {
            // Audio is an enhancement; timer behavior must continue if playback is unavailable.
        }
    }
}
