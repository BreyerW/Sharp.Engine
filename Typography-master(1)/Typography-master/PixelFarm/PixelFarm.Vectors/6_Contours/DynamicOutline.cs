﻿//MIT, 2017-present, WinterDev
using System;
using System.Collections.Generic;

namespace PixelFarm.Contours
{

    public class DynamicOutline
    {

        internal List<Contour> _contours;
        List<CentroidLine> _allCentroidLines;

        /// <summary>
        /// offset in pixel unit from master outline, accept + and -
        /// </summary>
        float _offsetFromMasterOutline = 0; //pixel unit
        float _pxScale;
        bool _needRefreshBoneGroup;
        bool _needAdjustGridFitValues;

        /// <summary>
        /// pixel-size-specific x offset (start from abstract origin) to fit the grid
        /// </summary>
        float _avg_x_fitOffset = 0;
        BoneGroupingHelper _groupingHelper;
        //
        public DynamicOutline(IntermediateOutline intermediateOutline)
        {

            //setup default values
            _needRefreshBoneGroup = true; //first time
            _needAdjustGridFitValues = true;//first time
            this.GridBoxWidth = 32; //pixels
            this.GridBoxHeight = 50; //pixels 
            _groupingHelper = BoneGroupingHelper.CreateBoneGroupingHelper();
#if DEBUG
            this.GridBoxHeight = dbugGridHeight; //pixels
            _dbugTempIntermediateOutline = intermediateOutline;
#endif

            //we convert data from GlyphIntermediateOutline to newform (lightweight form).
            //and save it here.
            //1. joints and its bones
            //2. bones and its controlled edge 
            _contours = intermediateOutline.GetContours(); //original contours
            //3.
            CollectAllCentroidLines(intermediateOutline.GetCentroidLineHubs());

        }
        private DynamicOutline()
        {
            //for empty dynamic outline

        }
        public static DynamicOutline CreateBlankDynamicOutline()
        {
            return new DynamicOutline();
        }

        public List<Contour> GetContours() => _contours;
        /// <summary>
        ///classify bone group by gridbox(w,h) 
        /// </summary>
        /// <param name="gridBoxW"></param>
        /// <param name="gridBoxH"></param>
        internal void AnalyzeBoneGroups(int gridBoxW, int gridBoxH)
        {
            //bone grouping depends on grid size.
            this.GridBoxHeight = gridBoxH;
            this.GridBoxWidth = gridBoxW;
            //
            _groupingHelper.Reset(gridBoxW, gridBoxH);

            for (int i = _allCentroidLines.Count - 1; i >= 0; --i)
            {
                //apply new grid to this centroid line 
                _groupingHelper.CollectBoneGroups(_allCentroidLines[i]);
            }
            //analyze bone group (stem) as a whole
            _groupingHelper.AnalyzeHorizontalBoneGroups();
            _groupingHelper.AnalyzeVerticalBoneGroups();

            //at this state we have a list of BoneGroup. 
            //but we don't know the  'fit-adjust' value for each GlyphPoint
            //we will known the 'fit-adjust' value after we know the pxscale
        }

        /// <summary>
        /// new stroke width offset from master outline
        /// </summary>
        /// <param name="offsetFromMasterOutline"></param>
        public void SetDynamicEdgeOffsetFromMasterOutline(float offsetFromMasterOutline)
        {

            if (_contours == null) return; //blank
            //preserve original outline
            //regenerate outline from original outline
            //----------------------------------------------------------        
            if (_offsetFromMasterOutline != offsetFromMasterOutline)
            {
                //change
                _offsetFromMasterOutline = offsetFromMasterOutline;
                //if 0, new other action
                List<Contour> cnts = _contours;
                for (int i = cnts.Count - 1; i >= 0; --i)
                {
                    cnts[i].ApplyNewEdgeOffsetFromMasterOutline(offsetFromMasterOutline);
                }
                //***
                //changing offset from master outline affects the grid fit-> need to recalculate 
                //_needRefreshBoneGroup = true;
            }
        }

        /// <summary>
        /// use grid fit or not
        /// </summary>
        public bool EnableGridFit { get; set; }
        /// <summary>
        /// grid box width in pixels
        /// </summary>
        public int GridBoxWidth { get; private set; }
        /// <summary>
        /// grid box height in pixels
        /// </summary>
        public int GridBoxHeight { get; private set; }

        public int MinX { get; private set; }
        public int MinY { get; private set; }
        public int MaxX { get; private set; }
        public int MaxY { get; private set; }
        public void SetOriginalGlyphControlBounds(int minX, int minY, int maxX, int maxY)
        {
            MinX = minX;
            MinY = minY;
            MaxX = maxX;
            MaxY = maxY;
        }
        /// <summary>
        /// original glyph advance width
        /// </summary>
        public int OriginalAdvanceWidth { get; set; }

        /// <summary>
        /// generate output with specific pixel scale
        /// </summary>
        /// <param name="tx"></param>
        /// <param name="pxScale"></param>
        public void GenerateOutput(IContourBuilder tx, float pxScale)
        {
            if (_contours == null) return; //blank
#if DEBUG
            this.EnableGridFit = dbugTestNewGridFitting;

#endif
            EnableGridFit = false;//temp fix, disable this value

            if (_pxScale != pxScale)
            {
                //new scale need to adjust fit value again
                _needAdjustGridFitValues = true;
            }
            //
            _pxScale = pxScale;
            //
            if (EnableGridFit)
            {
                if (_needRefreshBoneGroup)
                {
                    //change scale not affect the grid fit ***
                    AnalyzeBoneGroups(GridBoxWidth, GridBoxHeight);
                    _needRefreshBoneGroup = false;
                }
                //
                if (_needAdjustGridFitValues)
                {
                    ReCalculateFittingValues();
                    _needAdjustGridFitValues = false;
                }
            }

            if (tx != null)
            {
                List<Contour> contours = _contours;
                int j = contours.Count;
                tx.BeginRead(j);
                for (int i = 0; i < j; ++i)
                {
                    //generate in order of contour
                    GenerateContourOutput(tx, contours[i]);
                }
                tx.EndRead();
            }
        }

        struct FitDiffCollector
        {

            float _negative_diff;
            float _positive_diff;

            float _weighted_sum_negativeDiff;
            float _weighted_sum_positiveDiff;

            public void Collect(float diff, float groupLen)
            {
                //group len for weighting 
                if (diff < 0)
                {
                    _negative_diff += (diff * groupLen);
                    _weighted_sum_negativeDiff += groupLen;
                }
                else
                {
                    _positive_diff += (diff * groupLen);
                    _weighted_sum_positiveDiff += groupLen;
                }
            }

            public float CalculateProperDiff()
            {
                if (_weighted_sum_positiveDiff != 0 && _weighted_sum_negativeDiff != 0)
                {
                    //check if we should move to positive or negative  
                    //tech: choose minimum move to reach the target
                    if (_positive_diff > -_negative_diff)
                    {
                        //move to negative***
                        return -(_positive_diff - _negative_diff) / (_weighted_sum_positiveDiff + _weighted_sum_negativeDiff);
                    }
                    else
                    {
                        //avg to positive
                        return (_positive_diff - _negative_diff) / (_weighted_sum_positiveDiff + _weighted_sum_negativeDiff);
                    }
                }
                else if (_weighted_sum_positiveDiff != 0)
                {
                    //only positive side
                    return _positive_diff / _weighted_sum_positiveDiff;
                }
                else if (_weighted_sum_negativeDiff != 0)
                {
                    //only negative side, preserve negative sign
                    return _negative_diff / _weighted_sum_negativeDiff;
                }
                else
                {
                    return 0;
                }
            }


        }

        /// <summary>
        /// adjust vertical fitting value
        /// </summary>
        void ReCalculateFittingValues()
        {
            //(1) 
            //clear all prev adjust value
            for (int i = _contours.Count - 1; i >= 0; --i)
            {
                List<Vertex> pnts = _contours[i].flattenPoints;
                for (int m = pnts.Count - 1; m >= 0; --m)
                {
                    pnts[m].ResetFitAdjustValues();
                }
            }

            //adjust the value when we move to new pixel scale (pxscale)
            //if we known adjust values for that pxscale before( and cache it)
            //we can use that without recalculation

            //--------------------
            //(2)
            //select Horizontal BoneGroups for Vertical fitting:
            //for veritical fitting, we apply fitting value to each group.
            //each group may not need the same value.
            //--------------------
            List<BoneGroup> selectedHBoneGroups = _groupingHelper.SelectedHorizontalBoneGroups;
            for (int i = selectedHBoneGroups.Count - 1; i >= 0; --i)
            {

                BoneGroup boneGroup = selectedHBoneGroups[i];
                if (boneGroup._lengKind == BoneGroupSumLengthKind.Short)
                {
                    continue;
                }
                EdgeLine[] h_edges = boneGroup.edges;
                if (h_edges == null)
                {
                    continue;
                }
                //
                int edgeCount = h_edges.Length;
                //we need to calculate the avg of the glyph point
                //and add a total summary to this 
                FitDiffCollector y_fitDiffCollector = new FitDiffCollector();
                float groupLen = boneGroup.approxLength;
                //
                for (int e = edgeCount - 1; e >= 0; --e)
                {
                    EdgeLine ee = h_edges[e];
                    //p                    
                    y_fitDiffCollector.Collect(MyMath.CalculateDiffToFit(ee.P.Y * _pxScale), groupLen);
                    //q
                    y_fitDiffCollector.Collect(MyMath.CalculateDiffToFit(ee.Q.Y * _pxScale), groupLen);
                }

                float avg_ydiff = y_fitDiffCollector.CalculateProperDiff();

                for (int e = edgeCount - 1; e >= 0; --e)
                {

                    //TODO: review here again        
                    EdgeLine ee = h_edges[e];
                    ee.P.FitAdjustY = avg_ydiff;//assign px scale specific fit value
                    ee.Q.FitAdjustY = avg_ydiff;//assign px scale specific fit value
                }
            }
            //---------------------------------------------------------
            //(3)
            //vertical group for horizontal fit:
            //this different from the vertical fitting.
            //we calculate the value as a whole.
            //and apply it as a whole in later state 
            List<BoneGroup> verticalGroups = _groupingHelper.SelectedVerticalBoneGroups;
            FitDiffCollector x_fitDiffCollector = new FitDiffCollector();

            int j = verticalGroups.Count;
            for (int i = 0; i < j; ++i)
            {
                //1. the verticalGroup list is sorted, left to right
                //2. analyze in order left-> right

                BoneGroup boneGroup = verticalGroups[i];
                if (boneGroup._lengKind != BoneGroupSumLengthKind.Long)
                {
                    //in this case we focus on long-length bone group only
                    continue;
                }
                EdgeLine[] v_edges = boneGroup.edges;
                if (v_edges == null)
                {
                    continue;
                }

                int edgeCount = v_edges.Length;
                //we need to calculate the avg of the glyph point
                //and add a total summary to this 
                float groupLen = boneGroup.approxLength;
                for (int e = 0; e < edgeCount; ++e)
                {
                    EdgeLine ee = v_edges[e];
                    //TODO: review this
                    //if (ee.IsLeftSide)
                    //{
                    //focus on leftside edge
                    //p
                    x_fitDiffCollector.Collect(MyMath.CalculateDiffToFit(ee.P.X * _pxScale), groupLen);
                    //q
                    x_fitDiffCollector.Collect(MyMath.CalculateDiffToFit(ee.Q.X * _pxScale), groupLen);
                    //}
                }
                //TODO: review here ***
                break; //only left most first long group ?
            }
            //(4)
            _avg_x_fitOffset = x_fitDiffCollector.CalculateProperDiff();

        }
        /// <summary>
        /// pxscale-specific, average horizontal diff to fit the grid, this result come from fitting process
        /// </summary>
        public float AvgXFitOffset => _avg_x_fitOffset;
        //
        void CollectAllCentroidLines(List<CentroidLineHub> lineHubs)
        {
            //collect all centroid lines from each line CentroidLineHub
            _allCentroidLines = new List<CentroidLine>();
            int j = lineHubs.Count;
            for (int i = 0; i < j; ++i)
            {
                _allCentroidLines.AddRange(lineHubs[i].GetAllCentroidLines().Values);
            }
        }
        void GenerateContourOutput(IContourBuilder tx, Contour contour)
        {

            List<Vertex> points = contour.flattenPoints;
            int j = points.Count;
            if (j == 0) return;
            //------------------------------------------------- 
            //Bounds controlBounds = this.OriginalGlyphControlBounds;



            //walk along the edge in the contour to generate new edge output
            float pxscale = _pxScale;


#if DEBUG
            //            dbugWriteLine("===begin===" + fit_x_offset);
            //            if (!dbugUseHorizontalFitValue)
            //            {
            //                fit_x_offset = 0;
            //            }
#endif

            //------------------------------------------------- 
            //fineSubPixelRenderingOffset = 0.33f;

            //------------------------------------------------- 

            //TODO: review here 
            float fit_x, fit_y;
            points[0].GetFitXY(pxscale, out fit_x, out fit_y);
            //
            tx.MoveTo(fit_x, fit_y);
#if DEBUG
            // dbugWriteOutput("M", fit_x, fit_x + fit_x_offset, fit_y);
#endif
            //2. others
            for (int i = 1; i < j; ++i)
            {
                //try to fit to grid  
                points[i].GetFitXY(pxscale, out fit_x, out fit_y);
                tx.LineTo(fit_x, fit_y);
#if DEBUG
                //for debug
                //dbugWriteOutput("L", fit_x, fit_x + fit_x_offset, fit_y);
#endif
            }
            //close 
            tx.CloseContour();
#if DEBUG
            //dbugWriteLine("===end===");
#endif
        }
#if DEBUG
        void dbugWriteLine(string text)
        {
            //Console.WriteLine(text);
        }
        void dbugWriteOutput(string cmd, float pre_x, float post_x, float y)
        {
            // Console.WriteLine(cmd + "pre_x:" + pre_x + ",post_x:" + post_x + ",y" + y);
        }
        public static bool dbugActualPosToConsole { get; set; }
        public static bool dbugUseHorizontalFitValue { get; set; }
        public static bool dbugTestNewGridFitting { get; set; }
        public static int dbugGridHeight = 50;
        internal List<AnalyzedTriangle> dbugGetGlyphTriangles()
        {
            return _dbugTempIntermediateOutline.GetTriangles();
        }
        internal List<CentroidLineHub> dbugGetCentroidLineHubs()
        {
            return _dbugTempIntermediateOutline.GetCentroidLineHubs();
        }
        public IntermediateOutline _dbugTempIntermediateOutline;
        public bool dbugDrawRegeneratedOutlines { get; set; }
#endif

    }
}
