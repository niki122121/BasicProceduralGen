# BasicProceduralGen
**Generación Procedimental (Bloque III · Tema 15) — Run and Spawn**

Licencia: The MIT License

Este proyecto muestra cómo generar niveles en **Unity** usando un algoritmo tipo **Run and Spawn**:
uno o varios *runners* caminan por una cuadrícula y **dejan un rastro de celdas**; cada celda se
convierte después en una *room* (instancia de un prefab).

---

## Índice
1. [Requisitos](#requisitos)  
2. [Estructura del proyecto](#estructura-del-proyecto)  
3. [Puesta en marcha rápida](#puesta-en-marcha-rápida)  
4. [Cómo se usa en Unity](#cómo-se-usa-en-unity)  
5. [Algoritmo Run and Spawn (detalle)](#algoritmo-run-and-spawn-detalle)  
   - [Representación del espacio](#representación-del-espacio)  
   - [Estado de un runner](#estado-de-un-runner)  
   - [Bucle de generación](#bucle-de-generación)  
   - [Elección de giros y múltiples runners](#elección-de-giros-y-múltiples-runners)  
6. [Instanciación de rooms y cerramiento de puertas](#instanciación-de-rooms-y-cerramiento-de-puertas)  
7. [Rendimiento: activación dinámica](#rendimiento-activación-dinámica)  
8. [Extensiones y variantes](#extensiones-y-variantes)  
9. [Problemas comunes y soluciones](#problemas-comunes-y-soluciones)  
10. [Checklist de evaluación / Rúbrica](#checklist-de-evaluación--rúbrica)  
11. [Créditos y licencia](#créditos-y-licencia)

---

## Requisitos
- **Unity** (usa la misma versión que en la asignatura; cualquier LTS reciente debería funcionar).  
- **C#** y paquetes estándar de Unity (no se requieren paquetes externos).

---

## Estructura del proyecto
> Los nombres exactos pueden variar; esta es una guía típica para encontrar cada parte.

```
Assets/
  Scenes/                 # Escena(s) de demostración (p.ej., RunAndSpawnDemo.unity)
  Scripts/
    Generation/           # Scripts del algoritmo (Runner, RunAndSpawnGenerator, etc.)
    Runtime/              # Utilidades de activación dinámica, helpers, etc.
  Prefabs/
    Rooms/                # Prefabs de room (todas del mismo tamaño)
    Doors/                # Variantes de puerta/muro (opcional)
  Materials/              # Materiales (opcional)
README.md
```

---

## Puesta en marcha rápida
1) **Clona o descarga** este proyecto.  
2) Abre **Unity Hub** → **Open** → selecciona la carpeta del proyecto.  
3) Abre la escena de demo (p.ej., `Assets/Scenes/RunAndSpawnDemo.unity`).  
4) Pulsa **Play**.  
   - Deberías ver un nivel generado automáticamente con rooms en cuadrícula.  
   - En el **Inspector** del objeto *RunAndSpawnGenerator* puedes cambiar: _iterations_, _seed_, _runners_, _turnProbability_, _roomSize_, y los **prefabs de room**.

> Si el proyecto no trae una escena de demo, crea una vacía y sigue la sección **“Cómo se usa en Unity”**.

---

## Cómo se usa en Unity
1) **Crea un GameObject vacío** llamado `RunAndSpawnGenerator` y añade el script `RunAndSpawnGenerator.cs`.  
2) En el **Inspector** asigna:
   - **Room Prefab**: el prefab de habitación (todas las rooms deben tener el mismo tamaño).  
   - **Room Size**: tamaño mundial de cada room (ancho/largo en unidades Unity).  
   - **Iterations**: pasos totales de los runners (≈ tamaño del nivel).  
   - **Seed** (opcional): para resultados reproducibles (`Random.InitState(seed)`).  
   - **Runners**: lista de corredores con **posición inicial**, **dirección** inicial y **turnProbability**.  
3) (Opcional) Añade un botón UI que invoque `RunAndSpawnGenerator.Generate()` para regenerar en *runtime*.  
4) Pulsa **Play** para generar.  
5) (Opcional) Marca “**Close Orphan Doors**” para cerrar puertas sin vecino (ver más abajo).

---

## Algoritmo Run and Spawn (detalle)

### Representación del espacio
- Usamos una **cuadrícula lógica** donde cada celda representa la posición de una room.  
- Cada celda se identifica con `Vector2Int (x, y)`.  
- Guardamos las casillas visitadas en un `HashSet<Vector2Int>` para **evitar duplicados** cuando varios runners pisan la misma celda.

### Estado de un runner
```csharp
public struct Runner {
    public Vector2Int position;
    public Vector2Int direction;   // (0,1), (0,-1), (1,0) o (-1,0)
    public float turnProbability;  // probabilidad de girar por paso (0..1)
}
```

### Bucle de generación
```csharp
HashSet<Vector2Int> visited = new HashSet<Vector2Int>();
List<Runner> runners = CreateInitialRunners(); // p.ej., todos en (0,0)

for (int step = 0; step < iterations; step++) {
    for (int i = 0; i < runners.Count; i++) {
        var r = runners[i];

        // 1) Guardar celda
        visited.Add(r.position);

        // 2) Giro probabilístico
        if (UnityEngine.Random.value < r.turnProbability) {
            r.direction = ChooseNewDirection(r.direction);
        }

        // 3) Avanzar una celda
        r.position += r.direction;

        runners[i] = r;
    }
}
```
**Elección de nueva dirección.** La versión más simple elige una entre las 4 direcciones, distinta de la actual. Otra variante elige **solo giros de 90°** (izquierda/derecha) para trayectorias más “suaves”.  
**Número de iteraciones.** Controla el **tamaño aproximado** del nivel (más iteraciones ⇒ más rooms).

### Elección de giros y múltiples runners
- **Probabilidades distintas** por runner producen zonas con “pasillos largos” (poca probabilidad) y “laberintos densos” (probabilidad alta).  
- Varios runners iniciados en el centro generan un mapa en “ramas” que se alejan del origen.

Ejemplo de parámetros:
- 2 runners en (0,0): uno con `turnProbability = 0.1`, otro con `0.4`.  
- `iterations = 40`.  
- 2–3 prefabs de room para variedad visual.  
- La room más lejana al centro se marca como **jefe/premio**; el origen, **inicio**.

---

## Instanciación de rooms y cerramiento de puertas
Tras el paseo, recorre `visited` e **instancia** una room por celda.  
La posición mundial se calcula multiplicando coordenadas por el tamaño de la room.

```csharp
foreach (var cell in visited) {
    Vector3 worldPos = new Vector3(cell.x * roomSize.x, 0f, cell.y * roomSize.y); // top-down 3D
    Instantiate(roomPrefab, worldPos, Quaternion.identity, roomsParent);
}
```

Para **cerrar puertas sin vecino** (o sustituir por muro), comprueba la **vecindad** en 4 direcciones:
```csharp
static readonly Vector2Int[] DIRS = {
    new Vector2Int( 1, 0), // Este
    new Vector2Int(-1, 0), // Oeste
    new Vector2Int( 0, 1), // Norte
    new Vector2Int( 0,-1)  // Sur
};

bool HasNeighbor(HashSet<Vector2Int> visited, Vector2Int c, Vector2Int d)
    => visited.Contains(c + d);
```
En cada room, desactiva la puerta cuya celda vecina **no** exista en `visited`.  
Truco habitual: encuentra la **celda más alejada** del origen (distancia Manhattan) para ubicar el **jefe/premio**:
```csharp
Vector2Int FarthestFromOrigin(HashSet<Vector2Int> visited) {
    Vector2Int best = Vector2Int.zero;
    int bestDist = int.MinValue;
    foreach (var c in visited) {
        int dist = Mathf.Abs(c.x) + Mathf.Abs(c.y);
        if (dist > bestDist) { bestDist = dist; best = c; }
    }
    return best;
}
```

---

## Rendimiento: activación dinámica
Para mapas grandes, **no** mantengas cientos de rooms activas a la vez.  
Estrategia recomendada: instáncialas **desactivadas** y activa solo la room del jugador y sus vecinas.

```csharp
public class RoomActivator : MonoBehaviour {
    public Transform player;
    public float activateRadius = 1.1f; // en celdas
    public float cellWorldSize = 10f;   // ancho/largo de una room

    void Update() {
        // Activa por proximidad (en la práctica, usarías indices de celda para eficiencia)
        foreach (var room in allRooms) {
            float d = Vector3.Distance(player.position, room.transform.position);
            bool active = d <= activateRadius * cellWorldSize;
            if (room.activeSelf != active) room.SetActive(active);
        }
    }
}
```
Esto limita el coste en tiempo real sin cambiar la lógica del generador.

---

## Extensiones y variantes
- **Tamaño exacto de rooms**: en lugar de `for (step < iterations)`, usa `while (visited.Count < targetRooms)`.  
- **Generación 3D**: usa `Vector3Int (x,y,z)` y añade probabilidad de “giro vertical” (arriba/abajo).  
- **Caminos en mazmorras**: usa el paseo para trazar un **camino principal** y rellena alrededor con salas secundarias.  
- **Semillas**: añade `seed` para resultados deterministas (`Random.InitState(seed)`).  
- **Variedad visual**: alterna entre varios prefabs de room (paletas, props, iluminación).

---

## Problemas comunes y soluciones
- **Monotonía visual**: añade rotación aleatoria (90°) a rooms compatibles y mezcla varios prefabs.  
- **Rooms desalineadas**: asegúrate de que **todas** tienen exactamente el mismo tamaño mundial.  
- **Tamaño de nivel incontrolado**: cambia a condición de parada por `visited.Count` como arriba.  
- **Bordes abiertos**: revisa el post-proceso de puertas y la comprobación de vecindad.  
- **Rendimiento**: usa activación por proximidad y *pooling* de rooms si regeneras con frecuencia.  
- **Resultados poco interesantes**: combina runners con turnProbability distintas y ajusta `iterations`.

---

## Checklist de evaluación / Rúbrica
- ✅ **Instrucciones de uso**: abrir escena, configurar *generator*, ejecutar, regenerar.  
- ✅ **Explicación del algoritmo**: cuadrícula, runners, bucle, giro, múltiples agents.  
- ✅ **Instanciación y cierre de puertas**: convertir celdas en rooms, vecindad, puertas huérfanas.  
- ✅ **Rendimiento**: activación dinámica recomendada.  
- ✅ **Extensiones**: control de tamaño exacto, 3D, camino principal, semillas.  
- ✅ **Problemas comunes**: guía de resolución.

---

## Créditos y licencia
- Basado en los apuntes del **Bloque III · Tema 15: Generación Procedural** (*Run and Spawn*).  
- Uso educativo. Añade/consulta el archivo de licencia que corresponda al proyecto.
