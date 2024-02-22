
using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using System.Reflection;
using Random = UnityEngine.Random;

namespace FacundoColomboMethods
{
    public enum RaycastType
    {
        Sphere,
        Default
    }

    public enum MustBeOnSight
    {
        Yes,
        No
    }



    public static class ColomboMethods
    {
        static Dictionary<Tuple<float, int>, IEnumerable<Vector3>> PreCalculatedDirections = new();
        public static IEnumerable<Vector3> GetSpreadDirections(float maxAngle, int dirQuantity)
        {

            var key = Tuple.Create(maxAngle, dirQuantity);


            if (PreCalculatedDirections.ContainsKey(key))
            {
                var col = PreCalculatedDirections[key];
                foreach (var item in col)
                {
                    yield return item;
                }
                yield break;
            }
            Debug.LogWarning("Calculo Direcciones");
            FList<Vector3> newDirs = new FList<Vector3>();

            float halfAngle = maxAngle / 2;

            int halfDir = Mathf.Max(1, dirQuantity / 2);

            for (int i = -halfDir; i <= halfDir; i++)
            {
                float angle = halfAngle * i / halfDir;
                Vector3 currentDir = Quaternion.Euler(0, 0, angle) * Vector3.right;
                newDirs += currentDir;
                yield return currentDir;

            }
            PreCalculatedDirections.Add(key, newDirs);
        }

        //https://www.youtube.com/watch?v=ZC2iqxpSq7I&ab_channel=TheAshBot
        public static TComponent CopyComponent<TComponent>(this GameObject destination, TComponent OriginalComponent) where TComponent : Component
        {
            Type componentType = OriginalComponent.GetType();

            Component copy = destination.AddComponent(componentType);

            FieldInfo[] fields = componentType.GetFields();

            foreach (var f in fields)
            {
                f.SetValue(copy, f.GetValue(OriginalComponent));
            }

            return copy as TComponent;
        }

        //public static IEnumerable<Vector3> GetSpreadDirections(float maxAngle, int dirQuantity, Vector3 plane)
        //{
        //    dirQuantity = Mathf.Abs(dirQuantity);
        //    maxAngle = MathF.Abs(maxAngle);
        //    float halfAngle = maxAngle / 2;

        //    int halfDir = Mathf.Max(1, dirQuantity / 2);

        //    switch (dirQuantity)
        //    {
        //        //case < 0:
        //        //    yield return Vector3.zero;
        //        //    break;

        //        //case 0:
        //        //    yield return Vector3.zero;
        //        //    break;

        //        //case 1:
        //        //    yield return plane;
        //        //    break;

        //        //case 2:
        //        //    yield return plane;
        //        //    yield return Quaternion.Euler(Vector3.one * -halfDir).eulerAngles.MultiplyComponents(plane.normalized); ;
        //        //    break;


        //        default:
        //            for (int i = -halfDir; i < halfDir; i++)
        //            {
        //                float angle = halfAngle * i / halfDir;
        //                yield return Quaternion.AngleAxis(angle, plane).eulerAngles;

        //            }
        //            break;
        //    }


        //}

        public static IEnumerator DebugPositionInXSeconds(Rigidbody2D RB, float time)
        {

            float z = RB.transform.position.z;

            var gravity = Physics2D.gravity * RB.gravityScale * RB.mass;
            gravity /= 2;

            Vector2 initialPos = RB.position;

            Vector2 acceleration = RB.velocity * Time.deltaTime;

            FList<Vector3> list = new();
            list += initialPos;

            for (float i = 0; i <= time; i += Time.deltaTime)
            {

                //var acceleration = velocity1 / Time.deltaTime;
                float tpower = i * i;
                var currentgravity = gravity * tpower;
                Vector3 finalPos = initialPos + RB.velocity * i + (acceleration * tpower * 0.5f) + currentgravity;
                list += finalPos;
            }

            yield return null;
            var col = list.ToArray();
            for (int i = 1; i < col.Length; i++)
            {
                Debug.DrawLine(col[i - 1].SetZ(z), col[i].SetZ(z), Color.cyan, i);
            }



        }
        #region VectorExtension
        public static Vector3 MultiplyComponents(this Vector3 a, Vector3 b)
        {
            return new Vector3(a.x * b.x, a.y * b.y, a.z * b.z);
        }

        public static Vector3 GetMiddlePos(Vector3 initial, Vector3 final) => (final - initial) / 2 + initial;

        public static Vector2 ToVector2(this Vector3 value)
        {
            return new Vector2(value.x, value.y);
        }

        public static Vector3 ToVector3(this Vector2 value)
        {
            return new Vector3(value.x, value.y);
        }

        public static Vector3 SetZ(this Vector3 current, float newValue)
        {
            current.z = newValue;
            return current;
        }
        public static Vector3 SetY(this Vector3 current, float newValue)
        {
            current.y = newValue;
            return current;
        }

        public static Vector3 SetX(this Vector3 current, float newValue)
        {
            current.x = newValue;
            return current;
        }
        #endregion
        public static List<Camera> DeactivateAllCamerasExcept(this Camera exception)
        {
            var cams = Camera.allCameras
                .Where(x => x != exception)
                .Where(x => x.gameObject.activeInHierarchy).ToList();

            foreach (var item in cams.Select(x => x.gameObject))
            {
                item.SetActive(false);
            }
            return cams;
        }
        #region MouseShorcut
        //shortcut para unLockear mouse
        public static void UnlockMouse()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        //shortcut para Lockear mouse
        public static void LockMouse()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        #endregion
        //debe haber una manera mas facil de mover 1 item adelante en la list
        //o pasarlo para atras
        //alguna funcion de linq debe existir...
        public static List<T> MoveItems<T>(this List<T> list, float sign, int manyTimes = 1)
        {
            if (sign == 0) return list;

            //direccion a la que muevo los items
            int dir = (int)GetNearestNumber(sign, -1, 1);
            //hago un to list para crear una nueva instancia de la lista
            var ReOrderedList = list.ToList();
            // el largo de la coleccion(?) seria largo si queda en 0?
            int lenght = 0;
            //de donde empieza la coleccion
            int startFrom = 0;

            Func<float, bool> predicate = default;
            // si ordeno los items para atras entonces:
            //el indice 0 de la coleccion deberia valer el indice maximo de mi coleccion
            //si ordeno los items hacia adelante
            //el indice maximo debe valer lo mismo que el indice 0
            Action<T> SwapPlace = SwapPlace = x => ReOrderedList.Insert(startFrom, x);

            // si quiero mover los items hacia adelante
            if (dir == 1)
            {
                //el largo seria el maximo de la coleccion
                lenght = ReOrderedList.Count - 1;
                //empiezo de 0
                startFrom = 0;
                // y mi predicado seria mientras que el indice
                // sea menor a el largo de la coleccion
                predicate = index => index < lenght;

            }
            // si quiero mover los items hacia atras
            else if (dir == -1)
            {
                //mi largo seria 0
                lenght = 0;
                //empiezo desde el inicio de la coleccion
                startFrom = ReOrderedList.Count - 1;
                //y mi predicado es mientras que mi indice sea mayor a lenght
                //(el indice lo sumo o resto dependiendo de dir)
                predicate = index => index > lenght;
            }

            // itero las veces que quiera
            //(talvez podria poner un wait until o algo asi para que lo haga cada x frames
            // o espere x condicion para seguir iterando?)

            for (int j = manyTimes; j > 0; j--)
            {
                //me guardo el auxiliar de mi largo para despues remplazarlo 
                var swapAux = ReOrderedList[startFrom];
                for (int i = startFrom; predicate(i); i += dir)
                    ReOrderedList[i] = ReOrderedList[i + dir];
                // lo remplazo
                ReOrderedList[lenght] = swapAux;
            }


            return ReOrderedList;

        }

        public static float GetNearestNumber(float evaluated, float x, float y)
        {
            float distanceX = Mathf.Abs(evaluated - x);
            float distanceY = Mathf.Abs(evaluated - y);
            return distanceX > distanceY ? y : x;
        }
        #region GetChildrens
        //muy cabeza esto, ver como fixear
        public static bool HasChildren(this GameObject parent, GameObject target)
        {
            return parent.GetAllChildren().Where(x => x == target).Any();
        }

        public static IEnumerable<GameObject> GetAllChildren(this GameObject parent)
        {
            if (parent == null) return Enumerable.Empty<GameObject>();

            FList<GameObject> allChildren = new();
            allChildren += parent;

            //Debug.LogWarning(parent);
            if (parent.transform.childCount <= 0)
            {
                return allChildren;
            }
            foreach (Transform child in parent.transform)
            {
                // Check if the child has children
                allChildren += child.gameObject;
                if (child.childCount > 0)
                {

                    // Access the child's children using a recursive call
                    allChildren += child.gameObject.GetAllChildren();
                }
            }
            return allChildren;
        }

        public static IEnumerable<GameObject> GetAllChildren(this IEnumerable<GameObject> parents)
        {
            FList<GameObject> allChildren = new();
            foreach (var item in parents)
            {
                allChildren += item.GetAllChildren();
            }
            return allChildren;
        }
        #endregion
        //comentario de jocha:
        // ¿Por qué no usar Vector3.ProjectOnPlane?
        // Ese método hace lo mismo pero no asume que el vector normal proporcionado es de longitud unitaria;
        // Divide el resultado por la longitud al cuadrado de la normal, que siempre es 1, por lo que no es necesario.

        //este metodo NO es mio, tengo que informarme bien de proyecciones en plano.
        //no me gusta copiar cosas que no termino de entender, pero lo nesecitaba para prototipar
        //tengo una nocio de porque se usa pero no lo termino de entender
        public static Vector3 ProjectDirectionOnPlane(this Vector3 direction, Vector3 planeNormal)
        {
            return (direction - planeNormal * Vector3.Dot(direction, planeNormal)).normalized;
        }
        #region TriggerButtonEvents
        public static EventTrigger.Entry CreateEvent(Action method, EventTriggerType triggerType)
        {
            EventTrigger.Entry entry = new EventTrigger.Entry();
            entry.eventID = triggerType;
            entry.callback.AddListener(x => { method(); });
            return entry;

        }
        public static void CreateAndAddEvent(this EventTrigger trigger, Action method, EventTriggerType triggerType)
        {
            trigger.triggers.Add(CreateEvent(method, triggerType));
        }
        #endregion
        public static Vector3 CustomSmoothTransitionVector(this Vector3 actualPos, Vector3 from, Vector3 to, float velocity = 1, float floor = 1)
        {

            Vector3 dir = to - from;
            dir.Normalize();

            return actualPos + dir * actualPos.CustomSmoothTransitionFloat(from, to, velocity, floor);
        }

        public static float CustomSmoothTransitionFloat(this Vector3 actualPos, Vector3 from, Vector3 to, float velocity = 1, float floor = 1)
        {
            float lowest = Mathf.Min(Vector3.Distance(actualPos, from), Vector3.Distance(actualPos, to) + 0.001f);

            return Mathf.Clamp01(lowest * floor) * velocity * Time.deltaTime;
        }

        #region CameraExtensions
        public static bool IsInCameraSight(this Camera cam, Collider target)
        {
            var planes = GeometryUtility.CalculateFrustumPlanes(cam);
            //si el collider esta entre los 4 planos de la camara, devuelve verdadero
            return GeometryUtility.TestPlanesAABB(planes, target.bounds);
        }

        public static IEnumerable<T> AreTheyInCameraSight<T>(this Camera cam, IEnumerable<Tuple<T, Collider>> col)
        {
            var planes = GeometryUtility.CalculateFrustumPlanes(cam);

            foreach (var x in col)
                if (GeometryUtility.TestPlanesAABB(planes, x.Item2.bounds))
                    yield return x.Item1;

            //si el collider esta entre los 4 planos de la camara, devuelve verdadero

        }
        #endregion

        public static T CreateComponent<T>(this GameObject x) where T : Component
        {
            x.AddComponent(typeof(T));
            return x.GetComponent<T>();
        }

        public static Color SetAlpha(this Color color, float newAlpha)
        {
            return new Color(color.r, color.g, color.b, newAlpha);
        }

        public static Vector3 TryGetMeshCollision(this Vector3 myPos, Vector3 dir, LayerMask layer)
        {
            if (Physics.Raycast(myPos, dir, out RaycastHit hit, Mathf.Infinity, layer))
                return hit.point;
            else
                return myPos;
        }

        public static Tuple<float, Vector3> GetNormalAngleOfFloor(this Vector3 myPos, LayerMask layer)
        {
            if (Physics.Raycast(myPos, Vector3.down, out RaycastHit hit, Mathf.Infinity, layer))
            {
                return Tuple.Create<float, Vector3>(Vector3.Angle(hit.normal, Vector3.up), hit.normal);
            }
            return Tuple.Create<float, Vector3>(0, Vector3.zero);
        }

        #region OrientVectors
        public static Vector3 GetOrientedVector(this Transform tr, Vector3 T)
        {
            return T.GetOrientedVector(tr);
        }

        public static Vector3 GetOrientedVector(this Vector3 T, Transform tr)
        {
            return T.x * tr.right + T.y * tr.up + tr.forward * T.z;
        }

        #endregion


        public static string GenerateName(int len)
        {
            System.Random r = new System.Random();
            string[] consonants = { "b", "c", "d", "f", "g", "h", "j", "k", "l", "m", "l", "n", "p", "q", "r", "s", "sh", "zh", "t", "v", "w", "x" };
            string[] vowels = { "a", "e", "i", "o", "u", "ae", "y" };
            string Name = "";
            //el metodo Next es como un random range que va de 0 a x
            Name += consonants[r.Next(consonants.Length)].ToUpper();
            Name += vowels[r.Next(vowels.Length)];
            int b = 2; //b tells how many times a new letter has been added. It's 2 right now because the first two letters are already in the name.
            while (b < len)
            {
                Name += consonants[r.Next(consonants.Length)];
                b++;
                Name += vowels[r.Next(vowels.Length)];
                b++;
            }

            return Name;

        }
        /// <summary>
        /// devuelve el numero de la layer en la jerarquia de unity
        /// no se deberia usar si se tienen mas de 2 layers en la variable
        /// </summary>
        /// <param name="x"></param>
        /// <returns></returns>
        // esto es otra cosa que no termino de entender, no pude volver a encontrar el blog que lo explicaba
        // en algun momento le deberia tirar esto a chatgpt y que me explique la cuenta
        public static int LayerMaskToLayerNumber(this LayerMask x) => Mathf.RoundToInt(Mathf.Log(x, 2));

        public static int BitMaskToLayerNumber(this int x) => Mathf.RoundToInt(Mathf.Log(x, 2));



        public static void CheckAndAdd<T>(this List<T> col, T item)
        {
            if (!col.Contains(item))
            {
                col.Add(item);
            }
        }

        public static void CheckAndRemove<T>(this List<T> col, T item)
        {
            if (col.Contains(item)) col.Remove(item);
        }
        public static bool InBetween(this float value, float lessThan, float moreThan) => value <= lessThan && value >= moreThan;

        public static float InverseDistanceScalar(this Vector3 pos, Vector3 target, float radius)
        {
            // la distancia / el radio
            // pongo 1 - (valor entre 0 y 1) porque:
            // si esta en el limite de la distancia, la division me daria 1, por lo que recibiria la maxima fuerza si no lo restara por -1
            // y por si estoy en el centro, la division seria " 0 / 1" y en caso de que no le restara -1, no recibiria la maxima fuerza
            // deberia informarme mas de matematica para videojuegos, sin jocha no podria haber sacado esta "Magnitud Inversa"
            return Mathf.Max(1 - (Vector3.Distance(pos, target) / radius), 0f);
        }

        public static T[] GetItemsOFTypeAround<T>(this Vector3 pos, float radius)
        {
            return Physics.OverlapSphere(pos, radius)
                 .Select(x => x.GetComponent<T>())
                 .Where(x => x != null)
                 .ToArray();

        }

        //metodos utiles que puede ayudar a la hora de desarrollar el juego
        /// <summary>
        /// Soporta hasta 180 grados
        /// </summary>
        /// <param name="myDir"></param>
        /// <param name="angle"></param>
        /// <returns></returns>
        /// 
        public static Vector3 RandomDirFrom(this Vector3 myDir, float angle)
        {
            // el angulo maximo entre 2 vectores es 180, si se pasa vuelvea a empezar de 0
            Vector3 random = Random.insideUnitSphere;

            angle = Mathf.Clamp(MathF.Abs(angle), 0, 180);
            // lo divido por 180 para "Remapearlo a un valor mas bajo"     
            return Vector3.Slerp(myDir.normalized, random, angle / 180f); ;
        }

        public static List<T> CloneList<T>(List<T> listToClone)
        {
            List<T> aux = new List<T>();
            for (int i = 0; i < listToClone.Count; i++)
            {
                aux.Add(listToClone[i]);
            }
            return aux;
        }
        /// <summary>
        /// devuelve los componentes que tengas de hijos
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="Father"></param>
        /// <returns></returns>
        /// 
        public static T[] GetChildrenComponents<T>(this Transform Father)
        {
            var Components = new List<T>();
            for (int i = 0; i < Father.childCount; i++)
            {
                var item = Father.transform.GetChild(i).GetComponent<T>();
                if (item != null)
                {
                    Components.Add(item);
                }
            }
            return Components.ToArray();
        }
        /// <summary>
        /// obtiene todos los componentes de tipo T que haya cerca
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="tr"></param>
        /// <param name="radius"></param>
        /// <returns></returns>
        public static List<T> GetNearby<T>(this Transform tr, float radius)
        {
            List<T> list = new List<T>();
            Collider[] colliders = Physics.OverlapSphere(tr.position, radius);

            foreach (Collider Object in colliders)
            {
                T item = Object.GetComponent<T>();
                if (item != null)
                {
                    list.Add(item);
                }
            }


            return list;
        }
        /// <summary>
        ///  chequea si tenes algun objeto del tipo T a la vista y en cierto radio
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="pos"></param>
        /// <param name="radius"></param>
        /// <param name="Wall"></param>
        /// <returns></returns>
        public static bool CheckNearbyInSigth<T>(this Transform pos, float radius, LayerMask Wall) where T : MonoBehaviour
        {

            Collider[] colliders = Physics.OverlapSphere(pos.position, radius);
            foreach (Collider Object in colliders)
            {
                var item = Object.GetComponent<T>();
                if (item != null)
                {
                    if (InLineOfSight(pos.position, item.transform.position, Wall))
                    {
                        return true;
                    }

                }
            }

            return false;
        }

        /// <summary>
        /// Obtiene todos los componentes cercanos de tipo "T" que haya a la vista,
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="pos"></param>
        /// <param name="radius"></param>
        /// <param name="Wall"></param>
        /// <returns></returns>
        public static List<T> GetALLNearbyInSigth<T>(this Transform pos, float radius, LayerMask Wall) where T : MonoBehaviour
        {
            List<T> list = new List<T>();
            Collider[] colliders = Physics.OverlapSphere(pos.position, radius);

            foreach (Collider Object in colliders)
            {
                var item = Object.GetComponent<T>();
                if (item != null)
                {
                    if (InLineOfSight(pos.position, item.transform.position, Wall))
                    {
                        list.Add(item);
                    }

                }
            }

            return list;
        }

        /// <summary>
        /// chequea si tengo un punto en la vista o esta siendo obstruido por algo
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <param name="maskWall"></param>
        /// <returns></returns>
        public static bool InLineOfSight(this Vector3 start, Vector3 end, LayerMask maskWall)
        {
            Vector3 dir = end - start;

            return !Physics.Raycast(start, dir, dir.magnitude, maskWall);
        }

        public static bool InLineOfSight2D(this Vector2 start, Vector2 end, LayerMask maskWall)
        {
            Vector3 dir = end - start;

            return !Physics2D.Raycast(start, dir, dir.magnitude, maskWall);
        }

        public static bool InLineOffSight(this Vector3 start, Vector3 end, LayerMask maskWall, float distance)
        {
            Vector3 dir = end - start;

            bool InSight = !Physics.Raycast(start, dir, out RaycastHit info, distance, maskWall);
            if (info.point != Vector3.zero)
            {
                Debug.DrawLine(start, info.point);
            }
            return InSight;
        }

        public static Vector3 CheckForwardRayColision(Vector3 pos, Vector3 dir, float range = 15)
        {
            //aca se guardan los datos de con lo que impacte el rayo
            RaycastHit hit;

            //si el rayo choco contra algo
            if (Physics.Raycast(pos, dir, out hit, range))
            {
                return hit.point;

            }

            return new Vector3(pos.x, pos.y, pos.z + range);

        }
        #region se pueden hacer con linq mas facil D: :D

        /// <summary>
        /// obtiene el componente "T" mas cercano sin importar si esta a la vista o no
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="objPosition"></param>
        /// <param name="myPos"></param>
        /// <returns></returns>
        /// 

        //existe una funcion de linq para esto(order by)
        //public static T GetNearest<T>(T[] objPosition, Vector3 myPos) where T : MonoBehaviour
        //{
        //    int nearestIndex = 0;

        //    float nearestMagnitude = (objPosition[0].transform.position - myPos).magnitude;

        //    for (int i = 1; i < objPosition.Length; i++)
        //    {
        //        float tempMagnitude = (objPosition[i].transform.position - myPos).magnitude;

        //        if (nearestMagnitude > tempMagnitude)
        //        {
        //            nearestMagnitude = tempMagnitude;
        //            nearestIndex = i;
        //        }

        //    }

        //    return objPosition[nearestIndex];
        //}

        // existe una funcion de linq para esto(order By)
        //public static T GetFurthest<T>(this T[] objPosition, Vector3 myPos) where T : Transform
        //{
        //    int nearestIndex = 0;

        //    float nearestMagnitude = (objPosition[0].transform.position - myPos).magnitude;

        //    for (int i = 1; i < objPosition.Length; i++)
        //    {
        //        float tempMagnitude = (objPosition[i].transform.position - myPos).magnitude;

        //        if (nearestMagnitude > tempMagnitude)
        //        {
        //            nearestMagnitude = tempMagnitude;
        //            nearestIndex = i;
        //        }

        //    }

        //    return objPosition[nearestIndex];
        //}

        //public static T CheckNearest<T>(Transform[] objPosition, Vector3 myPos)
        //{
        //    int nearestIndex = 0;

        //    float nearestMagnitude = (objPosition[0].transform.position - myPos).magnitude;

        //    for (int i = 1; i < objPosition.Length; i++)
        //    {
        //        float tempMagnitude = (objPosition[i].transform.position - myPos).magnitude;

        //        if (nearestMagnitude > tempMagnitude)
        //        {
        //            nearestMagnitude = tempMagnitude;
        //            nearestIndex = i;
        //        }

        //    }

        //    return objPosition[nearestIndex].GetComponent<T>();
        //}
        /// <summary>
        /// obtiene el componente "T" mas cercano a la vista
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="objPosition"></param>
        /// <param name="myPos"></param>
        /// <param name="walls"></param>
        /// <returns></returns>
        public static T GetNearestOnSigth<T>(this Vector3 myPos, List<T> objPosition, LayerMask walls) where T : MonoBehaviour
        {
            List<T> listOnSigth = GetWhichAreOnSight(objPosition, myPos, walls);

            switch (listOnSigth.Count)
            {

                //ninguno a la vista
                case 0:
                    return null;
                //1 a la vista
                case 1:
                    return listOnSigth[0];
                //mas de  1 a la vista
                default:
                    float nearestMagnitude = (listOnSigth[0].transform.position - myPos).magnitude;
                    int nearestIndex = 0;

                    for (int i = 1; i < listOnSigth.Count; i++)
                    {
                        float tempMagnitude = (listOnSigth[i].transform.position - myPos).magnitude;

                        if (nearestMagnitude > tempMagnitude)
                        {
                            nearestMagnitude = tempMagnitude;
                            nearestIndex = i;
                        }

                    }

                    return listOnSigth[nearestIndex];


            }
        }

        /// <summary>
        /// devuelve todos los objetos de tipo "T" que esten a la vista
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="itemsPassed"></param>
        /// <param name="pos"></param>
        /// <param name="type"></param>
        /// <param name="wallMask"></param>
        /// <param name="radius"></param>
        /// <returns></returns>
        public static List<T> GetWhichAreOnSight<T>(this List<T> itemsPassed, Vector3 pos, LayerMask wallMask = default, RaycastType type = RaycastType.Default, float radius = 10f) where T : MonoBehaviour
        {

            switch (type)
            {
                case RaycastType.Sphere:
                    return FacundoSphereCastAll(pos, itemsPassed, radius, wallMask);

                default:
                    return FacundoRaycastAll(pos, itemsPassed);

            }

        }
        /// <summary>
        /// devuelve el objeto T si
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="item"></param>
        /// <param name="pos"></param>
        /// <param name="layer"></param>
        /// <param name="type"></param>
        /// <param name="sphereRadius"></param>
        /// <returns></returns>
        public static T Get_IsOnSight<T>(T item, Vector3 pos, LayerMask layer, RaycastType type = RaycastType.Default, float sphereRadius = 0) where T : MonoBehaviour
        {

            Vector3 dir = item.transform.position - pos;
            switch (type)
            {
                case RaycastType.Sphere:
                    if (FacundoSphereCast(pos, dir, sphereRadius, layer))
                    {
                        return item;
                    }
                    break;
                default:
                    if (FacundoRaycast<T>(pos, dir, item))
                    {
                        return item;
                    }

                    break;
            }
            return (T)default;

        }

        // mis raycasts
        static bool FacundoRaycast<T>(Vector3 pos, Vector3 dir, T item) where T : MonoBehaviour
        {
            RaycastHit hit;
            if (!Physics.Raycast(pos, dir, out hit, dir.magnitude))
            {
                return true;
            }
            else
            {
                Transform HitObject = hit.transform;
                if (HitObject == item.transform)
                {
                    return true;
                }
            }
            return false;


        }

        static List<T> FacundoRaycastAll<T>(Vector3 pos, List<T> items) where T : MonoBehaviour
        {
            List<T> Tlist = items.Where(x =>
            {
                Vector3 dir = x.transform.position - pos;
                return FacundoRaycast(pos, dir, x);
            }).ToList();

            return Tlist.Count >= 1 ? Tlist : new List<T>();



        }

        static bool FacundoSphereCast(Vector3 pos, Vector3 dir, float radius, LayerMask layer)
        {
            return !Physics.SphereCast(pos, radius, dir, out RaycastHit hit, dir.magnitude, layer);
        }

        static List<T> FacundoSphereCastAll<T>(Vector3 pos, List<T> items, float radius, LayerMask wallMask) where T : MonoBehaviour
        {
            return items.Where(x =>
            {
                Vector3 dir = x.transform.position - pos;
                return FacundoSphereCast(pos, dir, radius, wallMask);
            }).ToList();
        }

    }
    #endregion
}
