using System.Collections.Generic;
using System.Numerics;
using ShaderExample.Utils;

namespace ShaderExample
{
    class Model
    {
        private List<Entity> _entities;

        public IEnumerable<Entity> Entities => _entities;

        public Model()
        {
            _entities = new List<Entity>();

            //_entities.Add(new Entity(Enums.EntityType.Triangle, new Vector4(1f,0,0,1f),-Vector3.One, Vector3.Zero, new Vector3(2)));

            _entities.Add(new Entity(Enums.EntityType.Tetrahedron, new Vector4(1f, 0, 0, 1), new Vector3(0f, 0f, 0f), Vector3.Zero, new Vector3(0.7f)));
            _entities.Add(new Entity(Enums.EntityType.Tetrahedron, new Vector4(0f, 0, 1, 1), new Vector3(0.2f, 0f, -1f), Vector3.Zero, new Vector3(0.7f)));
            _entities.Add(new Entity(Enums.EntityType.Tetrahedron, new Vector4(0f, 1, 0, 1), new Vector3(2f, 0f, -4f), Vector3.Zero, new Vector3(0.7f)));
        }

        public void Update(float deltaTime)
        {
            _entities[0].Rotate(new Vector3(0, deltaTime, 0));
            _entities[1].Rotate(new Vector3(deltaTime, 0, -deltaTime));
            _entities[2].Rotate(new Vector3(-deltaTime, -deltaTime, deltaTime));
        }
    }
}
