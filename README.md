# Image Segmentation Project

## Overview

The Image Segmentation Project partitions an input image into distinct regions based on pixel similarity across RGB channels. It constructs three weighted graphs—one per channel (Red, Green, Blue)—where:

- **Nodes** are image pixels  
- **Edges** connect each pixel to its 8 neighbors (3×3 window)  
- **Edge weights** are the absolute difference in that channel’s intensity between two pixels  

Each graph is segmented independently, then the three segmentations are intersected to produce a final map of regions consistent across all channels.

---

## Key Concepts

### 1. Graph Construction

- Treat every pixel as a graph **node**.  
- Connect it to its 8 surrounding neighbors.  
- Assign each edge a **weight** = |intensity₁ − intensity₂| for the chosen color channel (R, G, or B).  
- Builds three separate graphs: Red, Green, Blue.

### 2. Image Segmentation Using DSU

- **Counting Sort** (O(M)) sorts the edges by weight (0–255) in linear time.  
- A **Disjoint Set Union** (Union-Find) groups pixels into components by uniting nodes connected by low-weight edges.  
- A merging threshold, based on edge weight and component size, follows Felzenszwalb & Huttenlocher’s method.  
- Produces three independent segmentations—one per channel.

### 3. Intersection of Segmentations

- Finds pixels that belong to the **same component** in all three channel segmentations.  
- Implements this by creating a new DSU and, in parallel, uniting neighbor pixels when their Red, Green, and Blue segment IDs all match.  
- The result is the **final segmentation** labeling.

### 4. Region Color Assignment & Output

- Assigns each final segment a **random RGB color** for visual distinction.  
- Updates the original image matrix with these segment colors.  
- Records each segment’s size (pixel count).  
- Optionally writes a log file (`logs/output.txt`) with:
  1. Total number of segments  
  2. Size of each segment  

---

## Code Workflow

1. **Graph Construction** (`GraphConstruct`)  
   - Loop over every pixel  
   - For each of its 8 neighbors, compute and store an `Edge(currentId, neighborId, weight)`

2. **Edge Sorting** (`CountingSortEdges`)  
   - Perform counting sort on edge weights in [0..255]

3. **Segmentation per Channel** (`ImageSegmentation`)  
   - Sort edges  
   - Use DSU to union connected pixels with low differences

4. **Parallel Graph Builds** (`ConstructGraphs`)  
   - Launch three tasks to build Red, Green, Blue graphs concurrently

5. **Parallel Segmentation** (`SegmentGraphs`)  
   - Segment Red and Green graphs in parallel; segment Blue sequentially

6. **Intersection**  
   - Create new DSU  
   - In parallel, for each pixel and its neighbors, if all three channel segment IDs match → union in intersection DSU

7. **Assign Colors**  
   - Generate a random color per segment  
   - Apply to the image matrix  
   - Count and sort segment sizes

8. **Logging** (`LogRegionInfoToFile`)  
   - Write segment count and sizes to file

9. **Manual Merge** (`MergeTwoSegments`)  
   - Allow users to select two segments, validate, then union & recolor

---

## Time Complexity Analysis

Let **W** = width, **H** = height, **N = W × H**, **E ≈ 8·N** edges.

| Step                                | Time Complexity            | Explanation                                                                    |
|-------------------------------------|----------------------------|--------------------------------------------------------------------------------|
| Graph Construction                  | O(N)                       | Each of N pixels has up to 8 neighbors → O(8N) = O(N)                          |
| Edge Sorting (Counting Sort)        | O(E + W) = O(N)          | Counting sort over fixed-weight range [0..255], E ≈ 8N                         |
| DSU Operations (`Union` over E edges) | O(E · α(N)) ≈ O(N)         | α(N) is inverse Ackermann (≈ constant)                                         |
| Intersection (parallel neighbor check) | O(N)                       | Each pixel checks up to 8 neighbors in parallel                                |
| Color Assignment & Mapping          | O(N)                       | One pass to assign colors and count sizes                                       |
| **Overall**                         | **O(N)**                   | All major steps are linear in the number of pixels                             |

**Space Complexity** is **O(N)** for:

- Pixel matrix  
- Edge lists (≈ 8N edges)  
- DSU structures  
- Segment color map  

