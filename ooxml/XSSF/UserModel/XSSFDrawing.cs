/* ====================================================================
   Licensed to the Apache Software Foundation (ASF) under one or more
   contributor license agreements.  See the NOTICE file distributed with
   this work for Additional information regarding copyright ownership.
   The ASF licenses this file to You under the Apache License, Version 2.0
   (the "License"); you may not use this file except in compliance with
   the License.  You may obtain a copy of the License at

       http://www.apache.org/licenses/LICENSE-2.0

   Unless required by applicable law or agreed to in writing, software
   distributed under the License is distributed on an "AS IS" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
   See the License for the specific language governing permissions and
   limitations under the License.
==================================================================== */

using System.Collections.Generic;
using System;
using NPOI.OpenXml4Net.OPC;
using NPOI.OpenXmlFormats.Dml;
using NPOI.XSSF.Model;
using System.IO;
using NPOI.OpenXmlFormats.Spreadsheet;
using NPOI.SS.UserModel;
using NPOI.OpenXmlFormats;
namespace NPOI.XSSF.UserModel
{

    /**
     * Represents a SpreadsheetML Drawing
     *
     * @author Yegor Kozlov
     */
    public class XSSFDrawing : POIXMLDocumentPart, IDrawing
    {
        /**
         * Root element of the SpreadsheetML Drawing part
         */
        private CT_Drawing drawing;
        private bool isNew;
        private long numOfGraphicFrames = 0L;

        protected static String NAMESPACE_A = "http://schemas.Openxmlformats.org/drawingml/2006/main";
        protected static String NAMESPACE_C = "http://schemas.Openxmlformats.org/drawingml/2006/chart";

        /**
         * Create a new SpreadsheetML Drawing
         *
         * @see NPOI.xssf.usermodel.XSSFSheet#CreateDrawingPatriarch()
         */
        protected XSSFDrawing()
            : base()
        {

            drawing = newDrawing();
            isNew = true;
        }

        /**
         * Construct a SpreadsheetML Drawing from a namespace part
         *
         * @param part the namespace part holding the Drawing data,
         * the content type must be <code>application/vnd.Openxmlformats-officedocument.Drawing+xml</code>
         * @param rel  the namespace relationship holding this Drawing,
         * the relationship type must be http://schemas.Openxmlformats.org/officeDocument/2006/relationships/drawing
         */
        protected XSSFDrawing(PackagePart part, PackageRelationship rel)
            : base(part, rel)
        {

            drawing = CT_Drawing.Parse(part.GetInputStream());
        }

        /**
         * Construct a new CT_Drawing bean. By default, it's just an empty placeholder for Drawing objects
         *
         * @return a new CT_Drawing bean
         */
        private static CT_Drawing newDrawing()
        {
            return new CT_Drawing();
        }

        /**
         * Return the underlying CT_Drawing bean, the root element of the SpreadsheetML Drawing part.
         *
         * @return the underlying CT_Drawing bean
         */

        public CT_Drawing GetCTDrawing()
        {
            return drawing;
        }


        protected void Commit()  {
 
        /*
            Saved Drawings must have the following namespaces Set:
            <xdr:wsDr
                xmlns:a="http://schemas.Openxmlformats.org/drawingml/2006/main"
                xmlns:xdr="http://schemas.Openxmlformats.org/drawingml/2006/spreadsheetDrawing">
        */
        //if(isNew) xmlOptions.SetSaveSyntheticDocumentElement(new QName(CT_Drawing.type.GetName().GetNamespaceURI(), "wsDr", "xdr"));
        Dictionary<String, String> map = new Dictionary<String, String>();
        map[NAMESPACE_A]= "a";
        map[ST_RelationshipId.NamespaceURI]= "r";
        //xmlOptions.SetSaveSuggestedPrefixes(map);

        PackagePart part = GetPackagePart();
        Stream out1 = part.GetOutputStream();
        drawing.Save(out1);
        out1.Close();
    }

        public XSSFClientAnchor CreateAnchor(int dx1, int dy1, int dx2, int dy2,
                int col1, int row1, int col2, int row2)
        {
            return new XSSFClientAnchor(dx1, dy1, dx2, dy2, col1, row1, col2, row2);
        }

        /**
         * Constructs a textbox under the Drawing.
         *
         * @param anchor    the client anchor describes how this group is attached
         *                  to the sheet.
         * @return      the newly Created textbox.
         */
        public XSSFTextBox CreateTextbox(XSSFClientAnchor anchor)
        {
            long shapeId = newShapeId();
            CT_TwoCellAnchor ctAnchor = CreateTwoCellAnchor(anchor);
            CT_Shape ctShape = ctAnchor.AddNewSp();
            ctShape.Set(XSSFSimpleShape.prototype());
            ctShape.GetNvSpPr().GetCNvPr().SetId(shapeId);
            XSSFTextBox shape = new XSSFTextBox(this, ctShape);
            shape.anchor = anchor;
            return shape;

        }

        /**
         * Creates a picture.
         *
         * @param anchor    the client anchor describes how this picture is attached to the sheet.
         * @param pictureIndex the index of the picture in the workbook collection of pictures,
         *   {@link NPOI.xssf.usermodel.XSSFWorkbook#getAllPictures()} .
         *
         * @return  the newly Created picture shape.
         */
        public XSSFPicture CreatePicture(XSSFClientAnchor anchor, int pictureIndex)
        {
            PackageRelationship rel = AddPictureReference(pictureIndex);

            long shapeId = newShapeId();
            CT_TwoCellAnchor ctAnchor = CreateTwoCellAnchor(anchor);
            CT_Picture ctShape = ctAnchor.AddNewPic();
            ctShape.Set(XSSFPicture.prototype());

            ctShape.nvPicPr.cNvPr.id = (uint)shapeId;

            XSSFPicture shape = new XSSFPicture(this, ctShape);
            shape.anchor = anchor;
            shape.SetPictureReference(rel);
            return shape;
        }

        public XSSFPicture CreatePicture(IClientAnchor anchor, int pictureIndex)
        {
            return CreatePicture((XSSFClientAnchor)anchor, pictureIndex);
        }

        /**
         * Creates a chart.
         * @param anchor the client anchor describes how this chart is attached to
         *               the sheet.
         * @return the newly Created chart
         * @see NPOI.xssf.usermodel.XSSFDrawing#CreateChart(ClientAnchor)
         */
        public XSSFChart CreateChart(XSSFClientAnchor anchor)
        {
            int chartNumber = GetPackagePart().Package.
                GetPartsByContentType(XSSFRelation.CHART.GetContentType()).Count + 1;

            XSSFChart chart = (XSSFChart)CreateRelationship(
                    XSSFRelation.CHART, XSSFFactory.GetInstance(), chartNumber);
            String chartRelId = chart.GetPackageRelationship().GetId();

            XSSFGraphicFrame frame = CreateGraphicFrame(anchor);
            frame.SetChart(chart, chartRelId);

            return chart;
        }

        public XSSFChart CreateChart(IClientAnchor anchor)
        {
            return CreateChart((XSSFClientAnchor)anchor);
        }

        /**
         * Add the indexed picture to this Drawing relations
         *
         * @param pictureIndex the index of the picture in the workbook collection of pictures,
         *   {@link NPOI.xssf.usermodel.XSSFWorkbook#getAllPictures()} .
         */
        protected PackageRelationship AddPictureReference(int pictureIndex)
        {
            XSSFWorkbook wb = (XSSFWorkbook)GetParent().GetParent();
            XSSFPictureData data = wb.GetAllPictures().Get(pictureIndex);
            PackagePartName ppName = data.GetPackagePart().GetPartName();
            PackageRelationship rel = GetPackagePart().AddRelationship(ppName, TargetMode.Internal, XSSFRelation.IMAGES.Relation);
            AddRelation(rel.GetId(), new XSSFPictureData(data.GetPackagePart(), rel));
            return rel;
        }

        /**
         * Creates a simple shape.  This includes such shapes as lines, rectangles,
         * and ovals.
         *
         * @param anchor    the client anchor describes how this group is attached
         *                  to the sheet.
         * @return  the newly Created shape.
         */
        public XSSFSimpleShape CreateSimpleShape(XSSFClientAnchor anchor)
        {
            long shapeId = newShapeId();
            CT_TwoCellAnchor ctAnchor = CreateTwoCellAnchor(anchor);
            CT_Shape ctShape = ctAnchor.AddNewSp();
            ctShape.Set(XSSFSimpleShape.prototype());
            ctShape.nvSpPr.cNvPr.id=(uint)(shapeId);
            XSSFSimpleShape shape = new XSSFSimpleShape(this, ctShape);
            shape.anchor = anchor;
            return shape;
        }

        /**
         * Creates a simple shape.  This includes such shapes as lines, rectangles,
         * and ovals.
         *
         * @param anchor    the client anchor describes how this group is attached
         *                  to the sheet.
         * @return  the newly Created shape.
         */
        public XSSFConnector CreateConnector(XSSFClientAnchor anchor)
        {
            CT_TwoCellAnchor ctAnchor = CreateTwoCellAnchor(anchor);
            CT_Connector ctShape = ctAnchor.AddNewCxnSp();
            ctShape.Set(XSSFConnector.prototype());

            XSSFConnector shape = new XSSFConnector(this, ctShape);
            shape.anchor = anchor;
            return shape;
        }

        /**
         * Creates a simple shape.  This includes such shapes as lines, rectangles,
         * and ovals.
         *
         * @param anchor    the client anchor describes how this group is attached
         *                  to the sheet.
         * @return  the newly Created shape.
         */
        public XSSFShapeGroup CreateGroup(XSSFClientAnchor anchor)
        {
            CT_TwoCellAnchor ctAnchor = CreateTwoCellAnchor(anchor);
            CT_GroupShape ctGroup = ctAnchor.AddNewGrpSp();
            ctGroup.Set(XSSFShapeGroup.prototype());

            XSSFShapeGroup shape = new XSSFShapeGroup(this, ctGroup);
            shape.anchor = anchor;
            return shape;
        }

        /**
         * Creates a comment.
         * @param anchor the client anchor describes how this comment is attached
         *               to the sheet.
         * @return the newly Created comment.
         */
        public XSSFComment CreateCellComment(IClientAnchor anchor)
        {
            XSSFClientAnchor ca = (XSSFClientAnchor)anchor;
            XSSFSheet sheet = (XSSFSheet)GetParent();

            //create comments and vmlDrawing parts if they don't exist
            CommentsTable comments = sheet.GetCommentsTable(true);
            XSSFVMLDrawing vml = sheet.GetVMLDrawing(true);
            schemasMicrosoftComVml.CT_Shape vmlShape = vml.newCommentShape();
            if (ca.IsSet())
            {
                String position =
                        ca.GetCol1() + ", 0, " + ca.GetRow1() + ", 0, " +
                        ca.GetCol2() + ", 0, " + ca.GetRow2() + ", 0";
                vmlShape.GetClientDataArray(0).SetAnchorArray(0, position);
            }
            XSSFComment shape = new XSSFComment(comments, comments.CreateComment(), vmlShape);
            shape.SetColumn(ca.GetCol1());
            shape.SetRow(ca.GetRow1());
            return shape;
        }

        /**
         * Creates a new graphic frame.
         *
         * @param anchor    the client anchor describes how this frame is attached
         *                  to the sheet
         * @return  the newly Created graphic frame
         */
        private XSSFGraphicFrame CreateGraphicFrame(XSSFClientAnchor anchor)
        {
            CT_TwoCellAnchor ctAnchor = CreateTwoCellAnchor(anchor);
            CT_GraphicalObjectFrame ctGraphicFrame = ctAnchor.AddNewGraphicFrame();
            ctGraphicFrame.Set(XSSFGraphicFrame.prototype());

            long frameId = numOfGraphicFrames++;
            XSSFGraphicFrame graphicFrame = new XSSFGraphicFrame(this, ctGraphicFrame);
            graphicFrame.SetAnchor(anchor);
            graphicFrame.SetId(frameId);
            graphicFrame.SetName("Diagramm" + frameId);
            return graphicFrame;
        }

        /**
         * Returns all charts in this Drawing.
         */
        public List<XSSFChart> GetCharts()
        {
            List<XSSFChart> charts = new List<XSSFChart>();
            foreach (POIXMLDocumentPart part in GetRelations())
            {
                if (part is XSSFChart)
                {
                    charts.Add((XSSFChart)part);
                }
            }
            return charts;
        }

        /**
         * Create and Initialize a CT_TwoCellAnchor that anchors a shape against top-left and bottom-right cells.
         *
         * @return a new CT_TwoCellAnchor
         */
        private CT_TwoCellAnchor CreateTwoCellAnchor(XSSFClientAnchor anchor)
        {
            CT_TwoCellAnchor ctAnchor = drawing.AddNewTwoCellAnchor();
            ctAnchor.from =(anchor.GetFrom());
            ctAnchor.to = (anchor.GetTo());
            ctAnchor.AddNewClientData();
            anchor.SetTo(ctAnchor.to);
            anchor.SetFrom(ctAnchor.from);
            ST_EditAs aditAs;
            switch (anchor.GetAnchorType())
            {
                case AnchorType.DONT_MOVE_AND_RESIZE: 
                    aditAs = ST_EditAs.absolute; break;
                case AnchorType.MOVE_AND_RESIZE: 
                    aditAs = ST_EditAs.twoCell; break;
                case AnchorType.MOVE_DONT_RESIZE: 
                    aditAs = ST_EditAs.oneCell; break;
                default: 
                    aditAs = ST_EditAs.oneCell;
                    break;
            }
            ctAnchor.editAs = (aditAs);
            return ctAnchor;
        }

        private long newShapeId()
        {
            return drawing.sizeOfTwoCellAnchorArray() + 1;
        }
    }
}
