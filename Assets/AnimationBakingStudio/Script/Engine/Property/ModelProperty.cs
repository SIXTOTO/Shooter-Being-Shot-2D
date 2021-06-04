using System;
using System.Collections.Generic;

namespace ABS
{
    [Serializable]
    public class ModelProperty : PropertyBase
    {
        public List<Model> list = new List<Model>();
    }
}
