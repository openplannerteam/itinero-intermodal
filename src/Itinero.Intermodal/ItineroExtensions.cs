using System;
using System.Collections.Generic;
using System.IO;
using Itinero.Attributes;
using Itinero.Data.Network;
using Itinero.IO.Json;
using Itinero.LocalGeo.IO;

namespace Itinero.Intermodal
{
    public static class ItineroExtensions
    {
        public static string ToGeoJson(this RoutingEdge edge, RouterDb db)
        {
            var edgeShape = db.Network.GetShape(edge);

            var writer = new StringWriter();
            var jsonWriter = new IO.Json.JsonWriter(writer);
            jsonWriter.WriteOpen();
            jsonWriter.WriteProperty("type", "FeatureCollection", true, false);
            jsonWriter.WritePropertyName("features", false);
            jsonWriter.WriteArrayOpen();

            jsonWriter.WriteOpen();
            jsonWriter.WriteProperty("type", "Feature", true, false);
            jsonWriter.WriteProperty("name", "ShapeMeta", true, false);
            jsonWriter.WritePropertyName("geometry", false);

            jsonWriter.WriteOpen();
            jsonWriter.WriteProperty("type", "LineString", true, false);
            jsonWriter.WritePropertyName("coordinates", false);
            jsonWriter.WriteArrayOpen();
            for (var shape = 0; shape < edgeShape.Count; shape++)
            {
                jsonWriter.WriteArrayOpen();
                jsonWriter.WriteArrayValue(edgeShape[shape].Longitude.ToInvariantString());
                jsonWriter.WriteArrayValue(edgeShape[shape].Latitude.ToInvariantString());
                if (edgeShape[shape].Elevation.HasValue)
                {
                    jsonWriter.WriteArrayValue(edgeShape[shape].Elevation.Value.ToInvariantString());
                }

                jsonWriter.WriteArrayClose();
            }

            jsonWriter.WriteArrayClose();
            jsonWriter.WriteClose();

            jsonWriter.WritePropertyName("properties");
            jsonWriter.WriteOpen();
            jsonWriter.WriteProperty("id", edge.Id.ToString(), false, false);
            jsonWriter.WriteProperty("profile", edge.Data.Profile.ToString(), false, false);
            jsonWriter.WriteProperty("meta", edge.Data.MetaId.ToString(), false, false);
            jsonWriter.WriteProperty("from", edge.From.ToString(), false, false);
            jsonWriter.WriteProperty("to", edge.To.ToString(), false, false);
            jsonWriter.WriteClose();

            jsonWriter.WriteClose();

            WriteVertex(db, jsonWriter, edge.From);
            WriteVertex(db, jsonWriter, edge.To);

            jsonWriter.WriteArrayClose();
            jsonWriter.WriteClose();

            return writer.ToString();
        }
        
        /// <summary>
        /// Writes a point-geometry for the given vertex.
        /// </summary>
        internal static void WriteVertex(this RouterDb db, JsonWriter jsonWriter, uint vertex)
        {
            var coordinate = db.Network.GetVertex(vertex);
            
            jsonWriter.WriteOpen();
            jsonWriter.WriteProperty("type", "Feature", true, false);
            jsonWriter.WritePropertyName("geometry", false);

            jsonWriter.WriteOpen();
            jsonWriter.WriteProperty("type", "Point", true, false);
            jsonWriter.WritePropertyName("coordinates", false);
            jsonWriter.WriteArrayOpen();
            jsonWriter.WriteArrayValue(coordinate.Longitude.ToInvariantString());
            jsonWriter.WriteArrayValue(coordinate.Latitude.ToInvariantString());
            jsonWriter.WriteArrayClose();
            jsonWriter.WriteClose();

            jsonWriter.WritePropertyName("properties");
            jsonWriter.WriteOpen();
            jsonWriter.WriteProperty("id", vertex.ToInvariantString());

            if (db.VertexData != null)
            {
                foreach (var dataName in db.VertexData.Names)
                {
                    var dataCollection = db.VertexData.Get(dataName);
                    if (vertex >= dataCollection.Count)
                    {
                        continue;
                    }
                    var data = dataCollection.GetRaw(vertex);
                    if (data != null)
                    {
                        jsonWriter.WriteProperty(dataName, data.ToInvariantString());
                    }
                }
            }

            var vertexMeta = db.VertexMeta?[vertex];
            if (vertexMeta != null)
            {
                foreach (var meta in vertexMeta)
                {
                    jsonWriter.WriteProperty(meta.Key, meta.Value, true, true);
                }
            }

            jsonWriter.WriteClose();

            jsonWriter.WriteClose();
        }
        
        /// <summary>
        /// Gets a geojson containing the given edge and optionally it's neighbours.
        /// </summary>
        /// <param name="db">The router db.</param>
        /// <param name="vertexIds">The vertices to get.</param>
        /// <param name="neighbours">Flag to get neighbours or not.</param>
        public static string GetGeoJsonVertices(this RouterDb db, bool neighbours, IEnumerable<uint> vertexIds)
        {
            var edgeEnumerator = db.Network.GetEdgeEnumerator();

            var writer = new StringWriter();

            var jsonWriter = new JsonWriter(writer);
            jsonWriter.WriteOpen();
            jsonWriter.WriteProperty("type", "FeatureCollection", true, false);
            jsonWriter.WritePropertyName("features", false);
            jsonWriter.WriteArrayOpen();

            // collect all edges and vertices to write.
            var vertices = new HashSet<uint>();
            var originalVertices = new HashSet<uint>(vertexIds);
            var edges = new HashSet<uint>();
            foreach (var vertex in vertexIds)
            {
                vertices.Add(vertex);

                if (neighbours)
                {
                    if (edgeEnumerator.MoveTo(vertex))
                    {
                        while (edgeEnumerator.MoveNext())
                        {
                            vertices.Add(edgeEnumerator.To);

                            edges.Add(edgeEnumerator.Id);
                        }
                    }
                }
                else
                {
                    if (edgeEnumerator.MoveTo(vertex))
                    {
                        while (edgeEnumerator.MoveNext())
                        {
                            if (originalVertices.Contains(edgeEnumerator.To))
                            {
                                edges.Add(edgeEnumerator.Id);
                            }
                        }
                    }
                }
            }

            foreach (var edgeId in edges)
            {
                edgeEnumerator.MoveToEdge(edgeId);
                db.WriteEdge(jsonWriter, edgeEnumerator);
            }

            foreach (var vertex in vertices)
            {
                db.WriteVertex(jsonWriter, vertex);
            }

            jsonWriter.WriteArrayClose();
            jsonWriter.WriteClose();

            return writer.ToString();
        }

        /// <summary>
        /// Writes a linestring-geometry for the edge currently in the enumerator.
        /// </summary>
        internal static void WriteEdge(this RouterDb db, JsonWriter jsonWriter, RoutingNetwork.EdgeEnumerator edgeEnumerator)
        {
            var edgeAttributes = new Itinero.Attributes.AttributeCollection(db.EdgeMeta.Get(edgeEnumerator.Data.MetaId));
            edgeAttributes.AddOrReplace(db.EdgeProfiles.Get(edgeEnumerator.Data.Profile));

            var shape = db.Network.GetShape(edgeEnumerator.Current);
            
            jsonWriter.WriteOpen();
            jsonWriter.WriteProperty("type", "Feature", true, false);
            jsonWriter.WritePropertyName("geometry", false);

            jsonWriter.WriteOpen();
            jsonWriter.WriteProperty("type", "LineString", true, false);
            jsonWriter.WritePropertyName("coordinates", false);
            jsonWriter.WriteArrayOpen();

            foreach (var coordinate in shape)
            {
                jsonWriter.WriteArrayOpen();
                jsonWriter.WriteArrayValue(coordinate.Longitude.ToInvariantString());
                jsonWriter.WriteArrayValue(coordinate.Latitude.ToInvariantString());
                jsonWriter.WriteArrayClose();
            }

            jsonWriter.WriteArrayClose();
            jsonWriter.WriteClose();

            jsonWriter.WritePropertyName("properties");
            jsonWriter.WriteOpen();
            if (edgeAttributes != null)
            {
                foreach (var attribute in edgeAttributes)
                {
                    jsonWriter.WriteProperty(attribute.Key, attribute.Value, true, true);
                }
            }
            jsonWriter.WriteProperty("edgeid", edgeEnumerator.Id.ToInvariantString());
            jsonWriter.WriteProperty("vertex1", edgeEnumerator.From.ToInvariantString());
            jsonWriter.WriteProperty("vertex2", edgeEnumerator.To.ToInvariantString());

            if (db.EdgeData != null)
            {
                foreach (var dataName in db.EdgeData.Names)
                {
                    var edgeId = edgeEnumerator.Id;
                    var dataCollection = db.EdgeData.Get(dataName);
                    if (edgeId >= dataCollection.Count)
                    {
                        continue;
                    }
                    var data = dataCollection.GetRaw(edgeId);
                    if (data != null)
                    {
                        jsonWriter.WriteProperty(dataName, data.ToInvariantString());
                    }
                }
            }

            jsonWriter.WriteClose();

            jsonWriter.WriteClose();
        }
    }
}
