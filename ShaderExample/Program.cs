using OpenTK;

namespace ShaderExample
{
    class Program
    {
        static void Main(string[] args)
        {
            GameWindow window = new GameWindow();
            Model model = new Model();
            View view = new View(false);

            window.UpdateFrame += (s, e) => model.Update((float)e.Time);
            window.RenderFrame += (s, e) => view.Render(model.Entities);
            window.RenderFrame += (s, e) => window.SwapBuffers();
            window.Resize += (s, e) => view.Resize(window.Width, window.Height);

            window.Run();
        }
    }
}
